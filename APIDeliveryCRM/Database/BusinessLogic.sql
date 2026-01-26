-- ============================================
-- БИЗНЕС-ЛОГИКА В БАЗЕ ДАННЫХ
-- Система управления службой доставки
-- ============================================

-- ============================================
-- 1. ОГРАНИЧЕНИЯ (CHECK CONSTRAINTS)
-- ============================================

-- Проверка: дата окончания смены должна быть после даты начала
ALTER TABLE "Courier_Shifts" 
ADD CONSTRAINT chk_shift_time 
CHECK ("TimeEnd" IS NULL OR "TimeEnd" > "TimeStart");

-- Проверка: рейтинг должен быть от 1 до 5
ALTER TABLE "Reviews" 
ADD CONSTRAINT chk_review_rating 
CHECK ("Rating" >= 1 AND "Rating" <= 5);

-- Проверка: вес, размеры и стоимость должны быть положительными
ALTER TABLE "Orders" 
ADD CONSTRAINT chk_order_weight 
CHECK ("Weight" > 0);

ALTER TABLE "Orders" 
ADD CONSTRAINT chk_order_dimensions 
CHECK ("Height" > 0 AND "Length" > 0 AND "Width" > 0);

ALTER TABLE "Orders" 
ADD CONSTRAINT chk_order_costs 
CHECK ("Estimated_cost" >= 0 AND "Final_cost" >= 0);

-- Проверка: адрес доставки не должен совпадать с адресом получения
ALTER TABLE "Orders" 
ADD CONSTRAINT chk_different_addresses 
CHECK ("PickupAddress_id" != "DeliveryAddress_id");

-- Проверка: рейтинг курьера должен быть от 0 до 5
ALTER TABLE "CourierProfiles" 
ADD CONSTRAINT chk_courier_rating 
CHECK ("Rating" >= 0 AND "Rating" <= 5);

-- Проверка: количество доставок не может быть отрицательным
ALTER TABLE "CourierProfiles" 
ADD CONSTRAINT chk_total_deliveries 
CHECK ("Total_deliveries" >= 0);

-- Проверка: дата доставки должна быть после даты создания заказа
ALTER TABLE "Orders" 
ADD CONSTRAINT chk_delivery_date 
CHECK ("Delivered_at" IS NULL OR "Delivered_at" >= "Created_at");

-- ============================================
-- 2. ФУНКЦИИ ДЛЯ БИЗНЕС-ЛОГИКИ
-- ============================================

-- Функция: Автоматический расчет рейтинга курьера на основе отзывов
-- Возвращает: TRIGGER
CREATE OR REPLACE FUNCTION update_courier_rating()
RETURNS TRIGGER AS $$
DECLARE
    avg_rating NUMERIC(5,2);
    courier_user_id INTEGER;
BEGIN
    -- Получаем ID пользователя курьера из заказа
    SELECT "Courier_id" INTO courier_user_id
    FROM "Orders"
    WHERE "ID_Order" = NEW."Order_id";
    
    IF courier_user_id IS NOT NULL THEN
        -- Вычисляем средний рейтинг
        SELECT COALESCE(AVG("Rating"), 0) INTO avg_rating
        FROM "Reviews"
        WHERE "TargetUser_id" = (
            SELECT "User_id" FROM "CourierProfiles" 
            WHERE "ID_CourierProfile" = courier_user_id
        );
        
        -- Обновляем рейтинг курьера
        UPDATE "CourierProfiles"
        SET "Rating" = ROUND(avg_rating::numeric, 2)
        WHERE "ID_CourierProfile" = courier_user_id;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Функция: Автоматическое обновление LastActivity_at при изменении координат курьера
-- Возвращает: TRIGGER
CREATE OR REPLACE FUNCTION update_courier_last_activity()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW."Current_lat" != OLD."Current_lat" OR NEW."Current_lon" != OLD."Current_lon" THEN
        NEW."LastActivity_at" = NOW();
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Функция: Автоматическое увеличение счетчика доставок при завершении заказа
-- Возвращает: TRIGGER
CREATE OR REPLACE FUNCTION increment_courier_deliveries()
RETURNS TRIGGER AS $$
BEGIN
    -- Если статус заказа изменился на "Доставлен" и курьер назначен
    IF NEW."Courier_id" IS NOT NULL AND NEW."Delivered_at" IS NOT NULL 
       AND (OLD."Delivered_at" IS NULL OR OLD."Delivered_at" != NEW."Delivered_at") THEN
        
        UPDATE "CourierProfiles"
        SET "Total_deliveries" = "Total_deliveries" + 1
        WHERE "ID_CourierProfile" = NEW."Courier_id";
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Функция: Проверка возможности назначения курьера на заказ
-- Возвращает: TRIGGER
CREATE OR REPLACE FUNCTION check_courier_availability()
RETURNS TRIGGER AS $$
DECLARE
    courier_status VARCHAR(100);
    courier_online BOOLEAN;
    active_shift_count INTEGER;
BEGIN
    IF NEW."Courier_id" IS NOT NULL THEN
        -- Проверяем статус курьера
        SELECT cs."Name", cp."Is_online" INTO courier_status, courier_online
        FROM "CourierProfiles" cp
        JOIN "Courier_statuses" cs ON cp."CurrentStatus_id" = cs."ID_CourierStatus"
        WHERE cp."ID_CourierProfile" = NEW."Courier_id";
        
        -- Проверяем наличие активной смены
        SELECT COUNT(*) INTO active_shift_count
        FROM "Courier_Shifts"
        WHERE "Courier_id" = NEW."Courier_id"
        AND "TimeEnd" IS NULL
        AND "Date" = CURRENT_DATE;
        
        -- Курьер должен быть онлайн и иметь активную смену
        IF NOT courier_online OR active_shift_count = 0 THEN
            RAISE EXCEPTION 'Курьер не может быть назначен: должен быть онлайн и иметь активную смену';
        END IF;
        
        -- Проверяем, что вес заказа не превышает возможности транспорта курьера
        DECLARE
            max_weight NUMERIC(10,2);
            order_weight NUMERIC(10,2);
        BEGIN
            SELECT vc."Max_Weight" INTO max_weight
            FROM "CourierProfiles" cp
            JOIN "Vehicle_categories" vc ON cp."VehicleCategory_id" = vc."ID_Category"
            WHERE cp."ID_CourierProfile" = NEW."Courier_id";
            
            SELECT "Weight" INTO order_weight FROM "Orders" WHERE "ID_Order" = NEW."ID_Order";
            
            IF order_weight > max_weight THEN
                RAISE EXCEPTION 'Вес заказа (%) превышает максимальную грузоподъемность транспорта курьера (%)', 
                    order_weight, max_weight;
            END IF;
        END;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Функция: Автоматическое создание уведомления при изменении статуса заказа
-- Возвращает: TRIGGER
    CREATE OR REPLACE FUNCTION create_order_status_notification()
    RETURNS TRIGGER AS $$
    DECLARE
        notification_type_id INTEGER;
        client_user_id INTEGER;
    BEGIN
        -- Если статус изменился
        IF NEW."Status_id" != OLD."Status_id" THEN
            -- Получаем ID типа уведомления "Изменение статуса заказа"
            SELECT "ID_NotificationType" INTO notification_type_id
            FROM "Notification_Types"
            WHERE "Name" = 'Изменение статуса заказа'
            LIMIT 1;
            
            -- Если тип уведомления не найден, создаем его
            IF notification_type_id IS NULL THEN
                INSERT INTO "Notification_Types" ("Name", "Description")
                VALUES ('Изменение статуса заказа', 'Уведомление об изменении статуса заказа')
                RETURNING "ID_NotificationType" INTO notification_type_id;
            END IF;
            
            -- Получаем ID пользователя клиента
            SELECT "User_id" INTO client_user_id
            FROM "ClientProfiles"
            WHERE "ID_ClientProfile" = NEW."Client_id";
            
            -- Создаем уведомление для клиента
            INSERT INTO "Notifications" ("User_id", "NotificationType_id", "Title", "Message", "Created_at", "Is_read")
            VALUES (
                client_user_id,
                notification_type_id,
                'Изменение статуса заказа',
                'Статус вашего заказа #' || NEW."Order_Number" || ' изменен',
                NOW(),
                false
            );
            
            -- Если курьер назначен, создаем уведомление и для него
            IF NEW."Courier_id" IS NOT NULL THEN
                DECLARE
                    courier_user_id INTEGER;
                BEGIN
                    SELECT "User_id" INTO courier_user_id
                    FROM "CourierProfiles"
                    WHERE "ID_CourierProfile" = NEW."Courier_id";
                    
                    INSERT INTO "Notifications" ("User_id", "NotificationType_id", "Title", "Message", "Created_at", "Is_read")
                    VALUES (
                        courier_user_id,
                        notification_type_id,
                        'Изменение статуса заказа',
                        'Статус заказа #' || NEW."Order_Number" || ' изменен',
                        NOW(),
                        false
                    );
                END;
            END IF;
        END IF;
        
        RETURN NEW;
    END;
    $$ LANGUAGE plpgsql;

    -- Функция: Автоматическое обновление LastMessage_at в чате
    -- Возвращает: TRIGGER
    CREATE OR REPLACE FUNCTION update_chat_last_message()
    RETURNS TRIGGER AS $$
    BEGIN
        UPDATE "ChatRooms"
        SET "LastMessage_at" = NEW."Sent_at"
        WHERE "ID_ChatRoom" = NEW."ChatRoom_id";
        
        RETURN NEW;
    END;
    $$ LANGUAGE plpgsql;

-- Функция: Автоматическая генерация номера заказа
-- Возвращает: TRIGGER
CREATE OR REPLACE FUNCTION generate_order_number()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW."Order_Number" IS NULL OR NEW."Order_Number" = 0 THEN
        NEW."Order_Number" := (
            SELECT COALESCE(MAX("Order_Number"), 0) + 1
            FROM "Orders"
        );
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Функция: Универсальный аудит для всех таблиц
-- Возвращает: TRIGGER
CREATE OR REPLACE FUNCTION audit_trigger_function()
RETURNS TRIGGER AS $$
DECLARE
    table_name_var VARCHAR(100);
    record_id_var INTEGER;
    action_var VARCHAR(20);
    field_name_var VARCHAR(500);
    old_val TEXT;
    new_val TEXT;
    description_var VARCHAR(500);
BEGIN
    -- Определяем имя таблицы
    table_name_var := TG_TABLE_NAME;
    action_var := TG_OP;
    
    -- Определяем ID записи в зависимости от таблицы
    IF TG_OP = 'INSERT' THEN
        CASE table_name_var
            WHEN 'Orders' THEN record_id_var := NEW."ID_Order";
            WHEN 'Users' THEN record_id_var := NEW."ID_User";
            WHEN 'CourierProfiles' THEN record_id_var := NEW."ID_CourierProfile";
            WHEN 'ClientProfiles' THEN record_id_var := NEW."ID_ClientProfile";
            WHEN 'ManagerProfile' THEN record_id_var := NEW."ID_ManagerProfile";
            ELSE record_id_var := 0;
        END CASE;
        
        description_var := 'Создана новая запись в таблице ' || table_name_var;
        
        INSERT INTO "AuditLog" ("TableName", "RecordId", "Action", "Description", "Created_at")
        VALUES (table_name_var, record_id_var, action_var, description_var, NOW());
        
        RETURN NEW;
        
    ELSIF TG_OP = 'UPDATE' THEN
        CASE table_name_var
            WHEN 'Orders' THEN record_id_var := NEW."ID_Order";
            WHEN 'Users' THEN record_id_var := NEW."ID_User";
            WHEN 'CourierProfiles' THEN record_id_var := NEW."ID_CourierProfile";
            WHEN 'ClientProfiles' THEN record_id_var := NEW."ID_ClientProfile";
            WHEN 'ManagerProfile' THEN record_id_var := NEW."ID_ManagerProfile";
            ELSE record_id_var := 0;
        END CASE;
        
        -- Отслеживаем изменения основных полей для заказов
        IF table_name_var = 'Orders' THEN
            IF NEW."Status_id" != OLD."Status_id" THEN
                field_name_var := 'Status_id';
                old_val := OLD."Status_id"::TEXT;
                new_val := NEW."Status_id"::TEXT;
                description_var := 'Изменен статус заказа';
                
                INSERT INTO "AuditLog" ("TableName", "RecordId", "Action", "FieldName", "OldValue", "NewValue", "Description", "Created_at")
                VALUES (table_name_var, record_id_var, action_var, field_name_var, old_val, new_val, description_var, NOW());
            END IF;
            
            IF NEW."Courier_id" IS DISTINCT FROM OLD."Courier_id" THEN
                field_name_var := 'Courier_id';
                old_val := COALESCE(OLD."Courier_id"::TEXT, 'NULL');
                new_val := COALESCE(NEW."Courier_id"::TEXT, 'NULL');
                description_var := CASE 
                    WHEN OLD."Courier_id" IS NULL THEN 'Курьер назначен на заказ'
                    WHEN NEW."Courier_id" IS NULL THEN 'Курьер отменен'
                    ELSE 'Курьер изменен'
                END;
                
                INSERT INTO "AuditLog" ("TableName", "RecordId", "Action", "FieldName", "OldValue", "NewValue", "Description", "Created_at")
                VALUES (table_name_var, record_id_var, action_var, field_name_var, old_val, new_val, description_var, NOW());
            END IF;
            
            IF NEW."Final_cost" != OLD."Final_cost" THEN
                field_name_var := 'Final_cost';
                old_val := OLD."Final_cost"::TEXT;
                new_val := NEW."Final_cost"::TEXT;
                description_var := 'Изменена стоимость заказа';
                
                INSERT INTO "AuditLog" ("TableName", "RecordId", "Action", "FieldName", "OldValue", "NewValue", "Description", "Created_at")
                VALUES (table_name_var, record_id_var, action_var, field_name_var, old_val, new_val, description_var, NOW());
            END IF;
        END IF;
        
        RETURN NEW;
        
    ELSIF TG_OP = 'DELETE' THEN
        CASE table_name_var
            WHEN 'Orders' THEN record_id_var := OLD."ID_Order";
            WHEN 'Users' THEN record_id_var := OLD."ID_User";
            WHEN 'CourierProfiles' THEN record_id_var := OLD."ID_CourierProfile";
            WHEN 'ClientProfiles' THEN record_id_var := OLD."ID_ClientProfile";
            WHEN 'ManagerProfile' THEN record_id_var := OLD."ID_ManagerProfile";
            ELSE record_id_var := 0;
        END CASE;
        
        description_var := 'Удалена запись из таблицы ' || table_name_var;
        
        INSERT INTO "AuditLog" ("TableName", "RecordId", "Action", "Description", "Created_at")
        VALUES (table_name_var, record_id_var, action_var, description_var, NOW());
        
        RETURN OLD;
    END IF;
    
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- 3. ТРИГГЕРЫ
-- ============================================

-- Подавление замечаний при удалении несуществующих триггеров
DO $$
BEGIN
    -- Триггер: Обновление рейтинга курьера при добавлении/изменении отзыва
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_update_courier_rating') THEN
        DROP TRIGGER trg_update_courier_rating ON "Reviews";
    END IF;
    
    -- Триггер: Обновление LastActivity_at при изменении координат курьера
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_update_courier_activity') THEN
        DROP TRIGGER trg_update_courier_activity ON "CourierProfiles";
    END IF;
    
    -- Триггер: Увеличение счетчика доставок при завершении заказа
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_increment_deliveries') THEN
        DROP TRIGGER trg_increment_deliveries ON "Orders";
    END IF;
    
    -- Триггер: Проверка возможности назначения курьера
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_check_courier_availability') THEN
        DROP TRIGGER trg_check_courier_availability ON "Orders";
    END IF;
    
    -- Триггер: Создание уведомления при изменении статуса заказа
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_order_status_notification') THEN
        DROP TRIGGER trg_order_status_notification ON "Orders";
    END IF;
    
    -- Триггер: Обновление LastMessage_at в чате
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_update_chat_last_message') THEN
        DROP TRIGGER trg_update_chat_last_message ON "ChatMessages";
    END IF;
    
    -- Триггер: Генерация номера заказа
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_generate_order_number') THEN
        DROP TRIGGER trg_generate_order_number ON "Orders";
    END IF;
    
    -- Триггеры аудита
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_orders_insert') THEN
        DROP TRIGGER trg_audit_orders_insert ON "Orders";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_orders_update') THEN
        DROP TRIGGER trg_audit_orders_update ON "Orders";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_orders_delete') THEN
        DROP TRIGGER trg_audit_orders_delete ON "Orders";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_users_insert') THEN
        DROP TRIGGER trg_audit_users_insert ON "Users";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_users_update') THEN
        DROP TRIGGER trg_audit_users_update ON "Users";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_users_delete') THEN
        DROP TRIGGER trg_audit_users_delete ON "Users";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_courier_profiles_insert') THEN
        DROP TRIGGER trg_audit_courier_profiles_insert ON "CourierProfiles";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_courier_profiles_update') THEN
        DROP TRIGGER trg_audit_courier_profiles_update ON "CourierProfiles";
    END IF;
    IF EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_audit_courier_profiles_delete') THEN
        DROP TRIGGER trg_audit_courier_profiles_delete ON "CourierProfiles";
    END IF;
END $$;

-- Триггер: Обновление рейтинга курьера при добавлении/изменении отзыва
CREATE TRIGGER trg_update_courier_rating
AFTER INSERT OR UPDATE OF "Rating" ON "Reviews"
FOR EACH ROW
EXECUTE FUNCTION update_courier_rating();

-- Триггер: Обновление LastActivity_at при изменении координат курьера
CREATE TRIGGER trg_update_courier_activity
BEFORE UPDATE OF "Current_lat", "Current_lon" ON "CourierProfiles"
FOR EACH ROW
EXECUTE FUNCTION update_courier_last_activity();

-- Триггер: Увеличение счетчика доставок при завершении заказа
CREATE TRIGGER trg_increment_deliveries
AFTER UPDATE OF "Delivered_at" ON "Orders"
FOR EACH ROW
EXECUTE FUNCTION increment_courier_deliveries();

-- Триггер: Проверка возможности назначения курьера
CREATE TRIGGER trg_check_courier_availability
BEFORE INSERT OR UPDATE OF "Courier_id" ON "Orders"
FOR EACH ROW
EXECUTE FUNCTION check_courier_availability();

-- Триггер: Создание уведомления при изменении статуса заказа
CREATE TRIGGER trg_order_status_notification
AFTER UPDATE OF "Status_id" ON "Orders"
FOR EACH ROW
EXECUTE FUNCTION create_order_status_notification();

-- Триггер: Обновление LastMessage_at в чате
CREATE TRIGGER trg_update_chat_last_message
AFTER INSERT ON "ChatMessages"
FOR EACH ROW
EXECUTE FUNCTION update_chat_last_message();

-- Триггер: Генерация номера заказа
CREATE TRIGGER trg_generate_order_number
BEFORE INSERT ON "Orders"
FOR EACH ROW
EXECUTE FUNCTION generate_order_number();

    -- ============================================
    -- 4. ИНДЕКСЫ ДЛЯ ПРОИЗВОДИТЕЛЬНОСТИ
    -- ============================================

-- Индекс для быстрого поиска заказов по клиенту
CREATE INDEX IF NOT EXISTS idx_orders_client ON "Orders"("Client_id");

-- Индекс для быстрого поиска заказов по курьеру
CREATE INDEX IF NOT EXISTS idx_orders_courier ON "Orders"("Courier_id") WHERE "Courier_id" IS NOT NULL;

-- Индекс для быстрого поиска заказов по статусу
CREATE INDEX IF NOT EXISTS idx_orders_status ON "Orders"("Status_id");

-- Индекс для поиска активных смен курьера
CREATE INDEX IF NOT EXISTS idx_shifts_active ON "Courier_Shifts"("Courier_id", "Date", "TimeEnd") 
WHERE "TimeEnd" IS NULL;

-- Индекс для поиска онлайн курьеров
CREATE INDEX IF NOT EXISTS idx_couriers_online ON "CourierProfiles"("Is_online", "CurrentStatus_id") 
WHERE "Is_online" = true;

-- Индекс для поиска непрочитанных уведомлений
CREATE INDEX IF NOT EXISTS idx_notifications_unread ON "Notifications"("User_id", "Is_read") 
WHERE "Is_read" = false;

-- Индекс для поиска сообщений в чате
CREATE INDEX IF NOT EXISTS idx_chat_messages_room ON "ChatMessages"("ChatRoom_id", "Sent_at");

-- Индекс для поиска участников чата
CREATE INDEX IF NOT EXISTS idx_chat_participants_user ON "ChatParticipants"("User_id");

-- Индексы для таблицы аудита
CREATE INDEX IF NOT EXISTS idx_audit_table_record ON "AuditLog"("TableName", "RecordId");
CREATE INDEX IF NOT EXISTS idx_audit_action ON "AuditLog"("Action", "Created_at");
CREATE INDEX IF NOT EXISTS idx_audit_user ON "AuditLog"("User_id", "Created_at") WHERE "User_id" IS NOT NULL;

-- ============================================
-- 5. ПРЕДСТАВЛЕНИЯ (VIEWS) ДЛЯ АНАЛИТИКИ
-- ============================================

-- Представление: Статистика по курьерам
CREATE OR REPLACE VIEW vw_courier_statistics AS
SELECT 
    cp."ID_CourierProfile",
    u."FName" || ' ' || u."Name" AS courier_name,
    cp."Rating",
    cp."Total_deliveries",
    cp."Is_online",
    cs."Name" AS status_name,
    COUNT(DISTINCT o."ID_Order") AS active_orders_count,
    COUNT(DISTINCT CASE WHEN o."Delivered_at" IS NOT NULL THEN o."ID_Order" END) AS completed_orders_count
FROM "CourierProfiles" cp
JOIN "Users" u ON cp."User_id" = u."ID_User"
JOIN "Courier_statuses" cs ON cp."CurrentStatus_id" = cs."ID_CourierStatus"
LEFT JOIN "Orders" o ON cp."ID_CourierProfile" = o."Courier_id"
GROUP BY cp."ID_CourierProfile", u."FName", u."Name", cp."Rating", cp."Total_deliveries", 
         cp."Is_online", cs."Name";

-- Представление: Статистика по заказам
CREATE OR REPLACE VIEW vw_order_statistics AS
SELECT 
    os."Name" AS status_name,
    COUNT(*) AS orders_count,
    COUNT(CASE WHEN o."Is_paid" = true THEN 1 END) AS paid_orders_count,
    SUM(o."Final_cost") AS total_revenue,
    AVG(o."Final_cost") AS avg_order_cost,
    AVG(EXTRACT(EPOCH FROM (o."Delivered_at" - o."Created_at")) / 3600) AS avg_delivery_hours
FROM "Orders" o
JOIN "Order_Statuses" os ON o."Status_id" = os."ID_OrderStatus"
GROUP BY os."Name";

-- Представление: Активные смены курьеров
CREATE OR REPLACE VIEW vw_active_shifts AS
SELECT 
    cs."ID_Shift",
    cp."ID_CourierProfile",
    u."FName" || ' ' || u."Name" AS courier_name,
    cs."Date",
    cs."TimeStart",
    EXTRACT(EPOCH FROM (NOW() - cs."TimeStart")) / 3600 AS hours_worked,
    ss."Name" AS shift_status
FROM "Courier_Shifts" cs
JOIN "CourierProfiles" cp ON cs."Courier_id" = cp."ID_CourierProfile"
JOIN "Users" u ON cp."User_id" = u."ID_User"
JOIN "Shift_Status" ss ON cs."ShiftStatus_id" = ss."ID_ShiftStatus"
WHERE cs."TimeEnd" IS NULL
AND cs."Date" = CURRENT_DATE;

-- ============================================
-- КОММЕНТАРИИ К ТАБЛИЦАМ И КОЛОНКАМ
-- ============================================

COMMENT ON TABLE "Orders" IS 'Заказы на доставку. Автоматически генерируется номер заказа при создании.';
COMMENT ON COLUMN "Orders"."Order_Number" IS 'Автоматически генерируемый уникальный номер заказа';
COMMENT ON COLUMN "CourierProfiles"."Rating" IS 'Автоматически рассчитывается на основе отзывов';
COMMENT ON COLUMN "CourierProfiles"."Total_deliveries" IS 'Автоматически увеличивается при завершении заказа';
COMMENT ON COLUMN "CourierProfiles"."LastActivity_at" IS 'Автоматически обновляется при изменении координат';


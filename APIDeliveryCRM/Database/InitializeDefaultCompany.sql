-- Скрипт для инициализации дефолтной компании и обновления существующих данных
-- Выполнить ПОСЛЕ применения миграции AddCompanyAndTenantId

-- 1. Создаем дефолтную компанию
INSERT INTO "Companies" (
    "Name", 
    "Subdomain", 
    "Created_at", 
    "Is_Active", 
    "SubscriptionPlan", 
    "MaxUsers", 
    "MaxOrdersPerMonth", 
    "SubscriptionExpiresAt"
)
VALUES (
    'Default Company',
    'default',
    NOW(),
    true,
    'Pro',
    100,
    10000,
    NOW() + INTERVAL '1 year'
)
RETURNING "ID_Company";

-- 2. Получаем ID созданной компании (предположим, это ID = 1)
-- Если компания уже существует, используем её ID

-- 3. Обновляем все существующие записи, устанавливая Company_id = 1
-- (замените 1 на реальный ID дефолтной компании, если он другой)

DO $$
DECLARE
    default_company_id INTEGER;
BEGIN
    -- Получаем ID дефолтной компании
    SELECT "ID_Company" INTO default_company_id
    FROM "Companies"
    WHERE "Subdomain" = 'default'
    LIMIT 1;
    
    -- Если компании нет, создаем её
    IF default_company_id IS NULL THEN
        INSERT INTO "Companies" (
            "Name", "Subdomain", "Created_at", "Is_Active", 
            "SubscriptionPlan", "MaxUsers", "MaxOrdersPerMonth", "SubscriptionExpiresAt"
        )
        VALUES (
            'Default Company', 'default', NOW(), true, 
            'Pro', 100, 10000, NOW() + INTERVAL '1 year'
        )
        RETURNING "ID_Company" INTO default_company_id;
    END IF;
    
    -- Обновляем все таблицы
    UPDATE "Users" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "Orders" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "ClientProfiles" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "CourierProfiles" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "ManagerProfiles" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "Addresses" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "ChatRooms" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "Notifications" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "Reports" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "Vehicles" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "Reviews" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "Courier_Shifts" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "FuelCards" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "AuditLogs" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    
    -- Для таблиц, которые могут быть добавлены позже
    UPDATE "Shift_Assignments" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "Vehicle_Assignments" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    UPDATE "Courier_FuelCards" SET "Company_id" = default_company_id WHERE "Company_id" = 0 OR "Company_id" IS NULL;
    
    RAISE NOTICE 'Default company ID: %', default_company_id;
    RAISE NOTICE 'All existing records updated with Company_id = %', default_company_id;
END $$;

-- 4. Проверяем результат
SELECT 
    'Users' as TableName, COUNT(*) as RecordsWithCompanyId
FROM "Users"
WHERE "Company_id" IS NOT NULL AND "Company_id" > 0
UNION ALL
SELECT 'Orders', COUNT(*) FROM "Orders" WHERE "Company_id" IS NOT NULL AND "Company_id" > 0
UNION ALL
SELECT 'ClientProfiles', COUNT(*) FROM "ClientProfiles" WHERE "Company_id" IS NOT NULL AND "Company_id" > 0
UNION ALL
SELECT 'CourierProfiles', COUNT(*) FROM "CourierProfiles" WHERE "Company_id" IS NOT NULL AND "Company_id" > 0;


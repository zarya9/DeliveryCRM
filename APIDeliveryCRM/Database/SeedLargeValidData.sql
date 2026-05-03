-- SeedLargeValidData.sql
-- Только независимые справочники и автопарк-справочники.
-- ВАЖНО: таблицы, где есть FK на Company/User, тут НЕ заполняются.
-- БД: PostgreSQL

BEGIN;

-- ------------------------------------------------------------
-- 1) Общие справочники (не зависят от Company/User)
-- ------------------------------------------------------------

INSERT INTO "Roles" ("Name")
SELECT v."Name"
FROM (VALUES
    ('Администратор'),
    ('Менеджер'),
    ('Логист'),
    ('Курьер'),
    ('Клиент')
) AS v("Name")
WHERE NOT EXISTS (
    SELECT 1 FROM "Roles" r WHERE r."Name" = v."Name"
);

INSERT INTO "PaymentMethods" ("Name")
SELECT v."Name"
FROM (VALUES
    ('Наличные'),
    ('Банковская карта'),
    ('Безналичный расчет'),
    ('СБП'),
    ('Корпоративный счет')
) AS v("Name")
WHERE NOT EXISTS (
    SELECT 1 FROM "PaymentMethods" pm WHERE pm."Name" = v."Name"
);

INSERT INTO "ClientStatuses" ("Name", "Code")
SELECT v."Name", v."Code"
FROM (VALUES
    ('Новый', 'NEW'),
    ('Активный', 'ACTIVE'),
    ('Проблемный', 'RISK'),
    ('VIP', 'VIP'),
    ('Неактивный', 'INACTIVE')
) AS v("Name", "Code")
WHERE NOT EXISTS (
    SELECT 1 FROM "ClientStatuses" cs WHERE cs."Code" = v."Code"
);

INSERT INTO "ClientSegments" ("Name", "Code")
SELECT v."Name", v."Code"
FROM (VALUES
    ('Физлица', 'B2C'),
    ('Малый бизнес', 'SMB'),
    ('Средний бизнес', 'MID'),
    ('Крупный бизнес', 'ENT'),
    ('Маркетплейс', 'MKT')
) AS v("Name", "Code")
WHERE NOT EXISTS (
    SELECT 1 FROM "ClientSegments" cs WHERE cs."Code" = v."Code"
);

INSERT INTO "ClientNoteTypes" ("Name", "Code")
SELECT v."Name", v."Code"
FROM (VALUES
    ('Общий комментарий', 'GENERAL'),
    ('Жалоба', 'COMPLAINT'),
    ('Пожелание', 'REQUEST'),
    ('Риск по оплате', 'PAYMENT_RISK'),
    ('Риск SLA', 'SLA_RISK')
) AS v("Name", "Code")
WHERE NOT EXISTS (
    SELECT 1 FROM "ClientNoteTypes" cnt WHERE cnt."Code" = v."Code"
);

INSERT INTO "OrderTypes" ("Name", "Description", "Base_price", "Price_km", "Estimated_delivery_factor")
SELECT v."Name", v."Description", v."Base_price", v."Price_km", v."Estimated_delivery_factor"
FROM (VALUES
    ('Экспресс', 'Срочная доставка в день заказа', 900.00, 45.00, 0.80),
    ('Стандарт', 'Плановая доставка в обычном режиме', 500.00, 28.00, 1.00),
    ('Межгород', 'Доставка между городами через хабы', 2500.00, 18.00, 1.70),
    ('Эконом', 'Экономичная доставка без SLA-приоритета', 350.00, 22.00, 1.20)
) AS v("Name", "Description", "Base_price", "Price_km", "Estimated_delivery_factor")
WHERE NOT EXISTS (
    SELECT 1 FROM "OrderTypes" ot WHERE ot."Name" = v."Name"
);

INSERT INTO "OrderStatuses" ("Name", "Description")
SELECT v."Name", v."Description"
FROM (VALUES
    ('Создан', 'Заказ создан в системе'),
    ('Принят', 'Заказ принят в работу'),
    ('Ожидает курьера', 'Ожидает назначения курьера'),
    ('В пути', 'Заказ передан в доставку'),
    ('На выдаче', 'Курьер на точке выдачи'),
    ('Доставлен', 'Заказ успешно завершен'),
    ('Отменен', 'Заказ отменен')
) AS v("Name", "Description")
WHERE NOT EXISTS (
    SELECT 1 FROM "OrderStatuses" os WHERE os."Name" = v."Name"
);

INSERT INTO "NotificationTypes" ("Name", "Description")
SELECT v."Name", v."Description"
FROM (VALUES
    ('OrderCreated', 'Создан новый заказ'),
    ('OrderStatusChanged', 'Изменен статус заказа'),
    ('SlaRisk', 'Риск нарушения SLA'),
    ('SlaBreach', 'Нарушение SLA'),
    ('SupportTicket', 'Событие по тикету поддержки'),
    ('Billing', 'Событие биллинга')
) AS v("Name", "Description")
WHERE NOT EXISTS (
    SELECT 1 FROM "NotificationTypes" nt WHERE nt."Name" = v."Name"
);

INSERT INTO "ChatRoomTypes" ("Name", "Description")
SELECT v."Name", v."Description"
FROM (VALUES
    ('Order', 'Чат по конкретному заказу'),
    ('Company', 'Общий корпоративный чат'),
    ('Support', 'Чат с поддержкой'),
    ('Direct', 'Личный чат между сотрудниками')
) AS v("Name", "Description")
WHERE NOT EXISTS (
    SELECT 1 FROM "ChatRoomTypes" crt WHERE crt."Name" = v."Name"
);

INSERT INTO "ShiftStatuses" ("Name", "Description")
SELECT v."Name", v."Description"
FROM (VALUES
    ('Запланирована', 'Смена создана, но еще не началась'),
    ('Активна', 'Смена в процессе'),
    ('Пауза', 'Временная пауза смены'),
    ('Завершена', 'Смена завершена'),
    ('Отменена', 'Смена отменена')
) AS v("Name", "Description")
WHERE NOT EXISTS (
    SELECT 1 FROM "ShiftStatuses" ss WHERE ss."Name" = v."Name"
);

INSERT INTO "ScheduleTypes" ("Name", "Description")
SELECT v."Name", v."Description"
FROM (VALUES
    ('5/2', 'Пять рабочих дней, два выходных'),
    ('2/2', 'Два через два'),
    ('Сутки/двое', '24 часа через 48 часов'),
    ('Гибкий', 'Индивидуальный график')
) AS v("Name", "Description")
WHERE NOT EXISTS (
    SELECT 1 FROM "ScheduleTypes" st WHERE st."Name" = v."Name"
);

INSERT INTO "CourierStatuses" ("Name", "Description")
SELECT v."Name", v."Description"
FROM (VALUES
    ('Свободен', 'Курьер доступен для назначения'),
    ('На заказе', 'Курьер выполняет доставку'),
    ('Оффлайн', 'Курьер недоступен'),
    ('На перерыве', 'Курьер временно недоступен'),
    ('Заблокирован', 'Курьер заблокирован администратором')
) AS v("Name", "Description")
WHERE NOT EXISTS (
    SELECT 1 FROM "CourierStatuses" cs WHERE cs."Name" = v."Name"
);

INSERT INTO "ReportStatuses" ("Name", "Description")
SELECT v."Name", v."Description"
FROM (VALUES
    ('Ожидает', 'Отчет создан и ожидает обработки'),
    ('В обработке', 'Отчет формируется'),
    ('Готов', 'Отчет успешно сформирован'),
    ('Ошибка', 'Ошибка при формировании отчета'),
    ('Отменен', 'Формирование отчета отменено')
) AS v("Name", "Description")
WHERE NOT EXISTS (
    SELECT 1 FROM "ReportStatuses" rs WHERE rs."Name" = v."Name"
);

INSERT INTO "LeadSources" ("Name", "Code")
SELECT v."Name", v."Code"
FROM (VALUES
    ('Сайт', 'WEB'),
    ('Реклама', 'ADS'),
    ('Рекомендация', 'REF'),
    ('Холодный звонок', 'COLD_CALL'),
    ('Маркетплейс', 'MKT')
) AS v("Name", "Code")
WHERE NOT EXISTS (
    SELECT 1 FROM "LeadSources" ls WHERE ls."Code" = v."Code"
);

INSERT INTO "LeadStages" ("Name", "Code", "SortOrder")
SELECT v."Name", v."Code", v."SortOrder"
FROM (VALUES
    ('Новый', 'NEW', 10),
    ('Квалификация', 'QUALIFY', 20),
    ('Переговоры', 'NEGOTIATION', 30),
    ('Договор', 'CONTRACT', 40),
    ('Выигран', 'WON', 50),
    ('Проигран', 'LOST', 60)
) AS v("Name", "Code", "SortOrder")
WHERE NOT EXISTS (
    SELECT 1 FROM "LeadStages" ls WHERE ls."Code" = v."Code"
);

INSERT INTO "FuelCardStatuses" ("Name", "IsCanBeUsed")
SELECT v."Name", v."IsCanBeUsed"
FROM (VALUES
    ('Активна', true),
    ('Временная блокировка', false),
    ('Заблокирована', false),
    ('Просрочена', false)
) AS v("Name", "IsCanBeUsed")
WHERE NOT EXISTS (
    SELECT 1 FROM "FuelCardStatuses" fcs WHERE fcs."Name" = v."Name"
);

INSERT INTO "FuelCardTypes" ("Name", "Priority")
SELECT v."Name", v."Priority"
FROM (VALUES
    ('Стандарт', 'Medium'),
    ('Премиум', 'High'),
    ('Резервная', 'Low')
) AS v("Name", "Priority")
WHERE NOT EXISTS (
    SELECT 1 FROM "FuelCardTypes" fct WHERE fct."Name" = v."Name"
);

INSERT INTO "FuelCompanies" ("Name", "PhoneManager", "DiscountPercent", "IsPreferred")
SELECT v."Name", v."PhoneManager", v."DiscountPercent", v."IsPreferred"
FROM (VALUES
    ('Gazprom Neft', '+7-495-700-00-01', 5.50, true),
    ('Lukoil', '+7-495-700-00-02', 4.80, false),
    ('Rosneft', '+7-495-700-00-03', 5.20, false),
    ('Tatneft', '+7-495-700-00-04', 4.40, false),
    ('Shell Fleet', '+7-495-700-00-05', 6.10, true)
) AS v("Name", "PhoneManager", "DiscountPercent", "IsPreferred")
WHERE NOT EXISTS (
    SELECT 1 FROM "FuelCompanies" fc WHERE fc."Name" = v."Name"
);

-- ------------------------------------------------------------
-- 2) Подробный автосправочник (FK-порядок соблюден)
-- ------------------------------------------------------------
-- Порядок: TransmissionTypes, DriveTypes, FuelTypes, VehicleBodyTypes,
--          VehicleCategories, VehicleBrands -> VehicleModels

INSERT INTO "TransmissionTypes" ("Name")
SELECT v."Name"
FROM (VALUES
    ('Механика'),
    ('Автомат'),
    ('Робот'),
    ('Вариатор')
) AS v("Name")
WHERE NOT EXISTS (
    SELECT 1 FROM "TransmissionTypes" tt WHERE tt."Name" = v."Name"
);

INSERT INTO "DriveTypes" ("Name")
SELECT v."Name"
FROM (VALUES
    ('Передний'),
    ('Задний'),
    ('Полный')
) AS v("Name")
WHERE NOT EXISTS (
    SELECT 1 FROM "DriveTypes" dt WHERE dt."Name" = v."Name"
);

INSERT INTO "FuelTypes" ("Name")
SELECT v."Name"
FROM (VALUES
    ('Бензин'),
    ('Дизель'),
    ('Газ'),
    ('Гибрид'),
    ('Электро')
) AS v("Name")
WHERE NOT EXISTS (
    SELECT 1 FROM "FuelTypes" ft WHERE ft."Name" = v."Name"
);

INSERT INTO "VehicleBodyTypes" ("Name")
SELECT v."Name"
FROM (VALUES
    ('Седан'),
    ('Хэтчбек'),
    ('Универсал'),
    ('Кроссовер'),
    ('Фургон'),
    ('Микроавтобус'),
    ('Пикап'),
    ('Рефрижератор')
) AS v("Name")
WHERE NOT EXISTS (
    SELECT 1 FROM "VehicleBodyTypes" vbt WHERE vbt."Name" = v."Name"
);

INSERT INTO "VehicleCategories" ("Name", "Description", "Max_Weight", "Speed_factor")
SELECT v."Name", v."Description", v."Max_Weight", v."Speed_factor"
FROM (VALUES
    ('Пеший', 'Курьер без транспорта', 5.00, 0.70),
    ('Вело', 'Велокурьер в городской зоне', 15.00, 0.95),
    ('Мото', 'Мотокурьер для плотного трафика', 25.00, 1.15),
    ('Легковая', 'Легковой автомобиль', 120.00, 1.00),
    ('Фургон LCV', 'Легкий коммерческий фургон', 1200.00, 0.90),
    ('Грузовая 3.5т', 'Среднетоннажная доставка', 3500.00, 0.80),
    ('Рефрижератор', 'Перевозка температурных грузов', 2500.00, 0.75)
) AS v("Name", "Description", "Max_Weight", "Speed_factor")
WHERE NOT EXISTS (
    SELECT 1 FROM "VehicleCategories" vc WHERE vc."Name" = v."Name"
);

INSERT INTO "VehicleBrands" ("Name")
SELECT v."Name"
FROM (VALUES
    ('LADA'),
    ('GAZ'),
    ('UAZ'),
    ('Hyundai'),
    ('Kia'),
    ('Toyota'),
    ('Nissan'),
    ('Volkswagen'),
    ('Ford'),
    ('Renault'),
    ('Peugeot'),
    ('Citroen'),
    ('Mercedes-Benz'),
    ('BMW'),
    ('Audi'),
    ('Skoda'),
    ('Geely'),
    ('Haval'),
    ('Changan'),
    ('Sollers'),
    ('Dongfeng'),
    ('JAC'),
    ('FAW')
) AS v("Name")
WHERE NOT EXISTS (
    SELECT 1 FROM "VehicleBrands" vb WHERE vb."Name" = v."Name"
);

-- Массовое заполнение моделей автомобилей
DO $$
DECLARE
    v_tm_mech   INTEGER;
    v_tm_auto   INTEGER;
    v_tm_cvt    INTEGER;
    v_dr_front  INTEGER;
    v_dr_rear   INTEGER;
    v_dr_awd    INTEGER;
BEGIN
    SELECT "ID_TransmisType" INTO v_tm_mech FROM "TransmissionTypes" WHERE "Name" = 'Механика' LIMIT 1;
    SELECT "ID_TransmisType" INTO v_tm_auto FROM "TransmissionTypes" WHERE "Name" = 'Автомат' LIMIT 1;
    SELECT "ID_TransmisType" INTO v_tm_cvt  FROM "TransmissionTypes" WHERE "Name" = 'Вариатор' LIMIT 1;

    SELECT "ID_DriveType" INTO v_dr_front FROM "DriveTypes" WHERE "Name" = 'Передний' LIMIT 1;
    SELECT "ID_DriveType" INTO v_dr_rear  FROM "DriveTypes" WHERE "Name" = 'Задний' LIMIT 1;
    SELECT "ID_DriveType" INTO v_dr_awd   FROM "DriveTypes" WHERE "Name" = 'Полный' LIMIT 1;

    -- LADA
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    CROSS JOIN (VALUES
        ('Granta', DATE '2021-01-01', 8.2, 5.7, 1.6, 106, v_tm_mech, v_dr_front),
        ('Vesta', DATE '2022-01-01', 8.8, 6.0, 1.6, 122, v_tm_auto, v_dr_front),
        ('Largus', DATE '2020-01-01', 9.1, 6.4, 1.6, 106, v_tm_mech, v_dr_front),
        ('Niva Travel', DATE '2023-01-01', 11.0, 8.4, 1.7, 80, v_tm_mech, v_dr_awd)
    ) AS x("Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
    WHERE b."Name" = 'LADA'
      AND NOT EXISTS (
          SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
      );

    -- GAZ
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    CROSS JOIN (VALUES
        ('Gazelle Next', DATE '2022-01-01', 12.5, 9.4, 2.8, 150, v_tm_mech, v_dr_rear),
        ('Sobol NN', DATE '2023-01-01', 11.8, 8.9, 2.5, 149, v_tm_mech, v_dr_rear)
    ) AS x("Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
    WHERE b."Name" = 'GAZ'
      AND NOT EXISTS (
          SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
      );

    -- Hyundai
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    CROSS JOIN (VALUES
        ('Solaris', DATE '2021-01-01', 8.4, 5.6, 1.6, 123, v_tm_auto, v_dr_front),
        ('Creta', DATE '2022-01-01', 9.6, 6.5, 2.0, 149, v_tm_auto, v_dr_awd),
        ('Staria', DATE '2023-01-01', 11.2, 7.9, 2.2, 177, v_tm_auto, v_dr_front)
    ) AS x("Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
    WHERE b."Name" = 'Hyundai'
      AND NOT EXISTS (
          SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
      );

    -- Kia
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    CROSS JOIN (VALUES
        ('Rio', DATE '2021-01-01', 8.3, 5.5, 1.6, 123, v_tm_auto, v_dr_front),
        ('Ceed SW', DATE '2022-01-01', 8.9, 6.1, 1.6, 128, v_tm_auto, v_dr_front),
        ('Sportage', DATE '2023-01-01', 10.1, 7.1, 2.0, 150, v_tm_auto, v_dr_awd)
    ) AS x("Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
    WHERE b."Name" = 'Kia'
      AND NOT EXISTS (
          SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
      );

    -- Toyota
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    CROSS JOIN (VALUES
        ('Corolla', DATE '2021-01-01', 7.8, 5.3, 1.6, 122, v_tm_cvt, v_dr_front),
        ('Camry', DATE '2022-01-01', 10.2, 6.8, 2.5, 181, v_tm_auto, v_dr_front),
        ('RAV4', DATE '2023-01-01', 9.7, 6.7, 2.0, 149, v_tm_cvt, v_dr_awd),
        ('Hilux', DATE '2021-01-01', 11.4, 8.1, 2.8, 200, v_tm_auto, v_dr_awd)
    ) AS x("Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
    WHERE b."Name" = 'Toyota'
      AND NOT EXISTS (
          SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
      );

    -- Volkswagen
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    CROSS JOIN (VALUES
        ('Polo', DATE '2021-01-01', 7.9, 5.4, 1.6, 110, v_tm_auto, v_dr_front),
        ('Passat', DATE '2022-01-01', 9.1, 6.2, 2.0, 190, v_tm_auto, v_dr_front),
        ('Transporter', DATE '2023-01-01', 10.8, 7.4, 2.0, 150, v_tm_mech, v_dr_front)
    ) AS x("Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
    WHERE b."Name" = 'Volkswagen'
      AND NOT EXISTS (
          SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
      );

    -- Ford
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    CROSS JOIN (VALUES
        ('Focus', DATE '2021-01-01', 8.5, 5.9, 1.6, 125, v_tm_auto, v_dr_front),
        ('Transit', DATE '2022-01-01', 11.6, 8.2, 2.2, 155, v_tm_mech, v_dr_rear),
        ('Ranger', DATE '2023-01-01', 12.1, 8.8, 2.0, 170, v_tm_auto, v_dr_awd)
    ) AS x("Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
    WHERE b."Name" = 'Ford'
      AND NOT EXISTS (
          SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
      );

    -- Mercedes-Benz
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    CROSS JOIN (VALUES
        ('C-Class', DATE '2022-01-01', 8.9, 6.0, 2.0, 204, v_tm_auto, v_dr_rear),
        ('Vito', DATE '2023-01-01', 10.7, 7.6, 2.0, 190, v_tm_auto, v_dr_rear),
        ('Sprinter', DATE '2022-01-01', 11.9, 8.5, 2.2, 190, v_tm_mech, v_dr_rear)
    ) AS x("Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
    WHERE b."Name" = 'Mercedes-Benz'
      AND NOT EXISTS (
          SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
      );

    -- Renault
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    CROSS JOIN (VALUES
        ('Logan', DATE '2021-01-01', 8.1, 5.8, 1.6, 113, v_tm_mech, v_dr_front),
        ('Duster', DATE '2022-01-01', 9.8, 6.9, 2.0, 143, v_tm_auto, v_dr_awd),
        ('Master', DATE '2023-01-01', 11.3, 8.3, 2.3, 150, v_tm_mech, v_dr_front)
    ) AS x("Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
    WHERE b."Name" = 'Renault'
      AND NOT EXISTS (
          SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
      );

    -- Китайские бренды
    INSERT INTO "VehicleModels" ("Brand_id", "Name", "Year", "AvgFuelCity", "AvgFuelHighWay", "EngineCapacity", "HorsePower", "TransmissionType_id", "DriveType_id")
    SELECT b."ID_Brand", x."Name", x."Year", x."AvgFuelCity", x."AvgFuelHighWay", x."EngineCapacity", x."HorsePower", x."TransmissionType_id", x."DriveType_id"
    FROM "VehicleBrands" b
    JOIN (VALUES
        ('Geely',   'Coolray', DATE '2023-01-01', 8.9, 6.3, 1.5, 150, v_tm_auto, v_dr_front),
        ('Geely',   'Atlas',   DATE '2022-01-01', 10.1, 7.2, 2.0, 200, v_tm_auto, v_dr_awd),
        ('Haval',   'Jolion',  DATE '2023-01-01', 9.2, 6.4, 1.5, 143, v_tm_auto, v_dr_front),
        ('Haval',   'F7',      DATE '2022-01-01', 10.4, 7.3, 2.0, 190, v_tm_auto, v_dr_awd),
        ('Changan', 'CS55',    DATE '2023-01-01', 9.5, 6.8, 1.5, 181, v_tm_auto, v_dr_front),
        ('JAC',     'N35',     DATE '2022-01-01', 11.2, 8.0, 2.0, 136, v_tm_mech, v_dr_rear),
        ('FAW',     'Bestune T77', DATE '2023-01-01', 8.8, 6.2, 1.5, 160, v_tm_auto, v_dr_front)
    ) AS x("BrandName","Name","Year","AvgFuelCity","AvgFuelHighWay","EngineCapacity","HorsePower","TransmissionType_id","DriveType_id")
      ON x."BrandName" = b."Name"
    WHERE NOT EXISTS (
        SELECT 1 FROM "VehicleModels" vm WHERE vm."Brand_id" = b."ID_Brand" AND vm."Name" = x."Name"
    );
END $$;

COMMIT;

-- Проверка (только по независимым таблицам)
SELECT 'Roles' AS "Table", COUNT(*) AS "Rows" FROM "Roles"
UNION ALL SELECT 'PaymentMethods', COUNT(*) FROM "PaymentMethods"
UNION ALL SELECT 'ClientStatuses', COUNT(*) FROM "ClientStatuses"
UNION ALL SELECT 'ClientSegments', COUNT(*) FROM "ClientSegments"
UNION ALL SELECT 'ClientNoteTypes', COUNT(*) FROM "ClientNoteTypes"
UNION ALL SELECT 'OrderTypes', COUNT(*) FROM "OrderTypes"
UNION ALL SELECT 'OrderStatuses', COUNT(*) FROM "OrderStatuses"
UNION ALL SELECT 'NotificationTypes', COUNT(*) FROM "NotificationTypes"
UNION ALL SELECT 'ChatRoomTypes', COUNT(*) FROM "ChatRoomTypes"
UNION ALL SELECT 'ShiftStatuses', COUNT(*) FROM "ShiftStatuses"
UNION ALL SELECT 'ScheduleTypes', COUNT(*) FROM "ScheduleTypes"
UNION ALL SELECT 'CourierStatuses', COUNT(*) FROM "CourierStatuses"
UNION ALL SELECT 'ReportStatuses', COUNT(*) FROM "ReportStatuses"
UNION ALL SELECT 'LeadSources', COUNT(*) FROM "LeadSources"
UNION ALL SELECT 'LeadStages', COUNT(*) FROM "LeadStages"
UNION ALL SELECT 'FuelCardStatuses', COUNT(*) FROM "FuelCardStatuses"
UNION ALL SELECT 'FuelCardTypes', COUNT(*) FROM "FuelCardTypes"
UNION ALL SELECT 'FuelCompanies', COUNT(*) FROM "FuelCompanies"
UNION ALL SELECT 'TransmissionTypes', COUNT(*) FROM "TransmissionTypes"
UNION ALL SELECT 'DriveTypes', COUNT(*) FROM "DriveTypes"
UNION ALL SELECT 'FuelTypes', COUNT(*) FROM "FuelTypes"
UNION ALL SELECT 'VehicleBodyTypes', COUNT(*) FROM "VehicleBodyTypes"
UNION ALL SELECT 'VehicleCategories', COUNT(*) FROM "VehicleCategories"
UNION ALL SELECT 'VehicleBrands', COUNT(*) FROM "VehicleBrands"
UNION ALL SELECT 'VehicleModels', COUNT(*) FROM "VehicleModels";

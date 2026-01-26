-- Скрипт для проверки и исправления связи ClientProfile -> PaymentMethod
-- Выполните этот скрипт, если получаете ошибку внешнего ключа

-- 1. Проверяем существование таблицы PaymentMethods (без подчеркивания после миграции RenameModels)
SELECT 
    table_name,
    table_schema
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('PaymentMethods', 'Payment_Methods');

-- 2. Проверяем существование внешнего ключа
SELECT 
    tc.constraint_name, 
    tc.table_name, 
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
    AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY' 
    AND tc.table_name = 'ClientProfiles'
    AND kcu.column_name = 'Preferred_payment_method_id';

-- 3. Проверяем данные в PaymentMethods
SELECT COUNT(*) as payment_methods_count FROM "PaymentMethods";
SELECT * FROM "PaymentMethods";

-- 4. Если таблица пуста, добавляем дефолтные способы оплаты
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "PaymentMethods" WHERE "Name" = 'Наличные') THEN
        INSERT INTO "PaymentMethods" ("Name") VALUES ('Наличные');
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM "PaymentMethods" WHERE "Name" = 'Банковская карта') THEN
        INSERT INTO "PaymentMethods" ("Name") VALUES ('Банковская карта');
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM "PaymentMethods" WHERE "Name" = 'Электронный кошелек') THEN
        INSERT INTO "PaymentMethods" ("Name") VALUES ('Электронный кошелек');
    END IF;
END $$;

-- 5. Проверяем записи ClientProfiles с несуществующими PaymentMethod
SELECT 
    cp."ID_ClientProfile",
    cp."Preferred_payment_method_id",
    pm."ID_PaymentMethod",
    pm."Name" as payment_method_name
FROM "ClientProfiles" cp
LEFT JOIN "PaymentMethods" pm ON cp."Preferred_payment_method_id" = pm."ID_PaymentMethod"
WHERE pm."ID_PaymentMethod" IS NULL;

-- 6. Если есть записи с несуществующими PaymentMethod, обновляем их на первый доступный
UPDATE "ClientProfiles" cp
SET "Preferred_payment_method_id" = (
    SELECT "ID_PaymentMethod" 
    FROM "PaymentMethods" 
    ORDER BY "ID_PaymentMethod" 
    LIMIT 1
)
WHERE NOT EXISTS (
    SELECT 1 
    FROM "PaymentMethods" pm 
    WHERE pm."ID_PaymentMethod" = cp."Preferred_payment_method_id"
);

-- 7. Финальная проверка
SELECT 
    'ClientProfiles' as table_name,
    COUNT(*) as total_records,
    COUNT(DISTINCT "Preferred_payment_method_id") as unique_payment_methods
FROM "ClientProfiles"
UNION ALL
SELECT 
    'PaymentMethods' as table_name,
    COUNT(*) as total_records,
    COUNT(*) as unique_payment_methods
FROM "PaymentMethods";


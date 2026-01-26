-- Проверка и создание начальных данных для Payment_Methods
-- Выполните этот скрипт, если получаете ошибку внешнего ключа

-- 1. Проверяем, существует ли таблица
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name = 'Payment_Methods'
) as table_exists;

-- 2. Проверяем, есть ли данные в таблице
SELECT COUNT(*) as record_count FROM "Payment_Methods";

-- 3. Если таблица пуста, добавляем дефолтные способы оплаты
INSERT INTO "Payment_Methods" ("Name")
SELECT 'Наличные'
WHERE NOT EXISTS (SELECT 1 FROM "Payment_Methods" WHERE "Name" = 'Наличные');

INSERT INTO "Payment_Methods" ("Name")
SELECT 'Банковская карта'
WHERE NOT EXISTS (SELECT 1 FROM "Payment_Methods" WHERE "Name" = 'Банковская карта');

INSERT INTO "Payment_Methods" ("Name")
SELECT 'Электронный кошелек'
WHERE NOT EXISTS (SELECT 1 FROM "Payment_Methods" WHERE "Name" = 'Электронный кошелек');

-- 4. Проверяем результат
SELECT * FROM "Payment_Methods";


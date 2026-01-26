# Анализ проблемы с внешним ключом PaymentMethod

## 🔍 Найденные проблемы

### 1. ❌ КРИТИЧЕСКАЯ: Отсутствует Company_id при создании ClientProfile

**Проблема:**
В методе `RegisterClientAsync` при создании `ClientProfile` не устанавливался обязательный `Company_id`, что вызывало ошибку при сохранении.

**Исправлено:**
- Добавлено получение дефолтной компании (ID = 1)
- Установлен `Company_id` для `User` и `ClientProfile`
- Исправлено также в `RegisterManagerAsync` и `RegisterCourierAsync`

### 2. ⚠️ Проблема с именем таблицы PaymentMethod

**Ситуация:**
- В БД таблица может называться `PaymentMethods` (без подчеркивания) после миграции `RenameModelsAndContextSecond`
- В конфигурации указано `.ToTable("PaymentMethods")` - правильно
- Но нужно убедиться, что миграции применены

**Проверка:**
```sql
-- Проверьте, какая таблица существует
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('PaymentMethods', 'Payment_Methods');
```

## ✅ Что исправлено

1. ✅ Добавлен `Company_id` при создании `ClientProfile`
2. ✅ Добавлен `Company_id` при создании `User` (во всех методах регистрации)
3. ✅ Добавлен `Company_id` при создании `ManagerProfile`
4. ✅ Добавлен `Company_id` при создании `CourierProfile`
5. ✅ Добавлена логика создания дефолтной компании, если её нет

## 🔧 Что нужно проверить в БД

### 1. Проверьте наличие таблицы PaymentMethods
```sql
SELECT * FROM "PaymentMethods";
```

### 2. Если таблица пуста, добавьте данные
```sql
INSERT INTO "PaymentMethods" ("Name") VALUES ('Наличные');
INSERT INTO "PaymentMethods" ("Name") VALUES ('Банковская карта');
```

### 3. Проверьте наличие дефолтной компании
```sql
SELECT * FROM "Companies" WHERE "ID_Company" = 1;
```

### 4. Если компании нет, создайте её
```sql
INSERT INTO "Companies" (
    "Name", "Subdomain", "Created_at", "Is_Active", 
    "SubscriptionPlan", "MaxUsers", "MaxOrdersPerMonth", "SubscriptionExpiresAt"
)
VALUES (
    'Default Company', 'default', NOW(), true, 
    'Pro', 100, 10000, NOW() + INTERVAL '1 year'
);
```

## 📝 Порядок действий

1. **Примените все миграции:**
   ```bash
   dotnet ef database update
   ```

2. **Проверьте данные в БД:**
   - Таблица `PaymentMethods` должна существовать и содержать данные
   - Таблица `Companies` должна содержать дефолтную компанию (ID = 1)

3. **Попробуйте зарегистрировать клиента снова**

## 🎯 Итог

Основная проблема была в отсутствии `Company_id` при создании `ClientProfile`. Теперь код автоматически:
- Получает или создает дефолтную компанию
- Устанавливает `Company_id` для всех создаваемых сущностей
- Создает дефолтный `PaymentMethod`, если его нет

Попробуйте снова - должно работать! 🚀


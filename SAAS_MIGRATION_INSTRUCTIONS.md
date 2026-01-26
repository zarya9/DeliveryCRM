# Инструкция по применению миграций для SaaS

## ✅ Что было сделано

1. ✅ Создана модель `Company` (компания-клиент)
2. ✅ Добавлен `Company_id` во все бизнес-модели:
   - User, Order, ClientProfiles, CourierProfiles, ManagerProfile
   - Address, ChatRoom, Notification, Report, Vehicle
   - Review, Courier_shifts, FuelCard, AuditLog
   - Shift_assignments, Vehicle_assignment, Courier_FuelCard
3. ✅ Обновлен `ContextDB` с настройками связей
4. ✅ Созданы миграции:
   - `AddCompanyAndTenantId` - основная миграция
   - `AddCompanyIdToRemainingTables` - для оставшихся таблиц

## 📋 Пошаговая инструкция

### Шаг 1: Применить миграции

```bash
cd APIDeliveryCRM
dotnet ef database update
```

**Что произойдет:**
- Создастся таблица `Companies`
- Добавится колонка `Company_id` во все таблицы
- Создастся дефолтная компания с ID = 1
- Все существующие записи получат `Company_id = 1`

### Шаг 2: Проверить результат

```bash
# Подключитесь к PostgreSQL
psql -h localhost -p 5555 -U postgres -d DeliveryCRM

# Проверьте, что компания создана
SELECT * FROM "Companies";

# Проверьте, что Company_id установлен
SELECT "ID_User", "Company_id", "FName", "Name" FROM "Users" LIMIT 5;
SELECT "ID_Order", "Company_id", "Order_Number" FROM "Orders" LIMIT 5;
```

### Шаг 3: (Опционально) Обновить существующие данные

Если нужно обновить данные вручную, выполните SQL скрипт:

```bash
psql -h localhost -p 5555 -U postgres -d DeliveryCRM -f APIDeliveryCRM/Database/InitializeDefaultCompany.sql
```

## ⚠️ Важно!

### Перед применением миграций:

1. **Сделайте бэкап БД:**
   ```bash
   pg_dump -h localhost -p 5555 -U postgres DeliveryCRM > backup_before_saas.sql
   ```

2. **Проверьте, что нет активных подключений к БД**

3. **Убедитесь, что приложение не запущено**

### После применения миграций:

1. **Проверьте данные:**
   ```sql
   -- Должна быть хотя бы одна компания
   SELECT COUNT(*) FROM "Companies";
   
   -- Все записи должны иметь Company_id > 0
   SELECT COUNT(*) FROM "Users" WHERE "Company_id" = 0 OR "Company_id" IS NULL;
   -- Должно вернуть 0
   ```

2. **Обновите код сервисов** для работы с `Company_id` (см. MULTI_TENANT_ARCHITECTURE.md)

## 🔄 Откат миграций (если нужно)

```bash
# Откатить последнюю миграцию
dotnet ef database update AddCompanyAndTenantId

# Или откатить все до определенной миграции
dotnet ef migrations remove
```

## 📊 Структура после миграции

### Таблица Companies

```sql
CREATE TABLE "Companies" (
    "ID_Company" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Subdomain" VARCHAR(100) UNIQUE,
    "LogoUrl" VARCHAR(500),
    "PrimaryColor" VARCHAR(50),
    "SecondaryColor" VARCHAR(50),
    "Created_at" TIMESTAMP NOT NULL,
    "Is_Active" BOOLEAN DEFAULT true,
    "SubscriptionPlan" VARCHAR(50) DEFAULT 'Basic',
    "MaxUsers" INTEGER DEFAULT 10,
    "MaxOrdersPerMonth" INTEGER DEFAULT 1000,
    "SubscriptionExpiresAt" TIMESTAMP NOT NULL,
    "AzureStorageConnectionString" VARCHAR(500),
    "AzureStorageContainerName" VARCHAR(100),
    "KafkaBootstrapServers" VARCHAR(500),
    "KafkaGroupId" VARCHAR(100)
);
```

### Обновленные таблицы

Все следующие таблицы теперь имеют `Company_id`:
- Users
- Orders
- ClientProfiles
- CourierProfiles
- ManagerProfiles
- Addresses
- ChatRooms
- Notifications
- Reports
- Vehicles
- Reviews
- Courier_Shifts
- FuelCards
- AuditLogs
- Shift_Assignments
- Vehicle_Assignments
- Courier_FuelCards

## 🎯 Следующие шаги

После применения миграций:

1. ✅ Создать `TenantResolutionMiddleware` (см. MULTI_TENANT_ARCHITECTURE.md)
2. ✅ Обновить JWT токен (добавить `CompanyId`)
3. ✅ Обновить сервисы для работы с Tenant
4. ✅ Настроить автоматическую фильтрацию в `ContextDB`

## ❓ Решение проблем

### Ошибка: "Foreign key constraint violation"

**Причина:** Миграция пытается добавить Foreign Key до создания таблицы Companies

**Решение:** Убедитесь, что миграция сначала создает таблицу Companies (исправлено в коде)

### Ошибка: "Column Company_id cannot be null"

**Причина:** Существующие записи не имеют Company_id

**Решение:** Миграция автоматически устанавливает `Company_id = 1` для всех записей

### Ошибка при применении миграции

**Решение:**
1. Откатите миграцию: `dotnet ef migrations remove`
2. Проверьте код моделей
3. Создайте миграцию заново: `dotnet ef migrations add AddCompanyAndTenantId`

---

**Готово!** БД теперь готова для SaaS архитектуры! 🚀


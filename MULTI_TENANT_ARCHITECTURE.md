# Multi-Tenant SaaS Архитектура для DeliveryCRM

## 🎯 Концепция

**Multi-Tenant (SaaS)** - одна система, множество компаний-клиентов.

```
┌─────────────────────────────────────────────────────────┐
│              Единая система DeliveryCRM                 │
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  Компания 1  │  │  Компания 2  │  │  Компания 3  │  │
│  │ "Доставка+"  │  │ "Быстрая"    │  │ "Экспресс"   │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│                                                          │
│  Все используют одно ПО, но данные изолированы          │
└─────────────────────────────────────────────────────────┘
```

---

## 📊 Варианты архитектуры Multi-Tenant

### Вариант 1: Shared Database, Shared Schema (Рекомендуется)

**Суть:** Одна БД, все компании в одной схеме, изоляция через `TenantId`.

**Преимущества:**
- ✅ Проще в разработке и поддержке
- ✅ Дешевле (одна БД)
- ✅ Легко масштабировать
- ✅ Проще делать бэкапы

**Недостатки:**
- ⚠️ Нужна тщательная изоляция данных
- ⚠️ Сложнее миграции (влияют на всех)

**Когда использовать:** Для большинства случаев (рекомендуется)

### Вариант 2: Shared Database, Separate Schema

**Суть:** Одна БД, но каждая компания в своей схеме.

**Преимущества:**
- ✅ Лучшая изоляция данных
- ✅ Можно кастомизировать схему для компании

**Недостатки:**
- ❌ Сложнее управление
- ❌ Больше ресурсов

**Когда использовать:** Когда нужна кастомизация схемы

### Вариант 3: Separate Database

**Суть:** Каждая компания в своей БД.

**Преимущества:**
- ✅ Максимальная изоляция
- ✅ Легко мигрировать компанию

**Недостатки:**
- ❌ Очень дорого
- ❌ Сложно масштабировать
- ❌ Сложнее обновления

**Когда использовать:** Для крупных корпоративных клиентов

---

## 🏗️ Рекомендуемая архитектура (Вариант 1)

### 1. Добавление Tenant (Компания) в модель данных

#### Новая модель: Company (Tenant)

```csharp
// APIDeliveryCRM/Model/Company.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIDeliveryCRM.Model
{
    public class Company
    {
        [Key]
        public int ID_Company { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;  // "Доставка+"
        
        [MaxLength(100)]
        public string? Subdomain { get; set; }  // "dostavka" → dostavka.deliverycrm.com
        
        [MaxLength(500)]
        public string? LogoUrl { get; set; }
        
        [MaxLength(50)]
        public string? PrimaryColor { get; set; }  // Цвет темы
        
        [MaxLength(50)]
        public string? SecondaryColor { get; set; }
        
        public DateTime Created_at { get; set; }
        public bool Is_Active { get; set; } = true;
        
        // Настройки подписки
        public string SubscriptionPlan { get; set; } = "Basic";  // Basic, Pro, Enterprise
        public int MaxUsers { get; set; } = 10;
        public int MaxOrdersPerMonth { get; set; } = 1000;
        public DateTime SubscriptionExpiresAt { get; set; }
        
        // Azure настройки (каждая компания может иметь свой Storage)
        [MaxLength(500)]
        public string? AzureStorageConnectionString { get; set; }
        
        [MaxLength(100)]
        public string? AzureStorageContainerName { get; set; }
        
        // Kafka настройки (опционально, для изоляции)
        [MaxLength(500)]
        public string? KafkaBootstrapServers { get; set; }
        
        [MaxLength(100)]
        public string? KafkaGroupId { get; set; }
    }
}
```

#### Обновление всех моделей: добавление TenantId

```csharp
// Пример: User.cs
public class User
{
    [Key]
    public int ID_User { get; set; }
    
    // ✅ Добавить TenantId во все модели
    [Required]
    [ForeignKey(nameof(Company))]
    public int Company_id { get; set; }
    public Company Company { get; set; } = null!;
    
    // ... остальные поля
}

// Пример: Order.cs
public class Order
{
    [Key]
    public int ID_Order { get; set; }
    
    // ✅ Добавить TenantId
    [Required]
    [ForeignKey(nameof(Company))]
    public int Company_id { get; set; }
    public Company Company { get; set; } = null!;
    
    // ... остальные поля
}
```

**Модели, которые нужно обновить:**
- ✅ User
- ✅ Order
- ✅ ClientProfiles
- ✅ CourierProfiles
- ✅ ManagerProfile
- ✅ Address
- ✅ ChatRoom
- ✅ Notification
- ✅ Report
- ✅ Vehicle
- ✅ И все остальные бизнес-сущности

---

### 2. Tenant Resolution (Определение компании)

#### Способ 1: По поддомену (Рекомендуется)

```
dostavka.deliverycrm.com  → Company_id = 1
bystraya.deliverycrm.com  → Company_id = 2
express.deliverycrm.com   → Company_id = 3
```

#### Способ 2: По заголовку HTTP

```
X-Tenant-Id: 1
```

#### Способ 3: По JWT токену

```csharp
// JWT токен содержит Company_id
{
  "userId": 123,
  "companyId": 1,  // ✅ Добавить
  "email": "user@example.com",
  "role": "Manager"
}
```

**Рекомендация:** Комбинация поддомена + JWT токена (двойная проверка безопасности)

---

### 3. Middleware для Tenant Resolution

```csharp
// APIDeliveryCRM/Middleware/TenantResolutionMiddleware.cs
using APIDeliveryCRM.ContextDb;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ContextDB dbContext)
        {
            int? companyId = null;

            // Способ 1: Из поддомена
            var host = context.Request.Host.Host;
            if (host.Contains('.'))
            {
                var subdomain = host.Split('.')[0];
                var company = await dbContext.Companies
                    .FirstOrDefaultAsync(c => c.Subdomain == subdomain);
                companyId = company?.ID_Company;
            }

            // Способ 2: Из заголовка (для API)
            if (!companyId.HasValue && context.Request.Headers.ContainsKey("X-Tenant-Id"))
            {
                if (int.TryParse(context.Request.Headers["X-Tenant-Id"], out var headerCompanyId))
                {
                    companyId = headerCompanyId;
                }
            }

            // Способ 3: Из JWT токена (если есть)
            if (!companyId.HasValue && context.User.Identity?.IsAuthenticated == true)
            {
                var companyIdClaim = context.User.FindFirst("CompanyId")?.Value;
                if (int.TryParse(companyIdClaim, out var tokenCompanyId))
                {
                    companyId = tokenCompanyId;
                }
            }

            // Сохраняем в HttpContext для использования в сервисах
            if (companyId.HasValue)
            {
                context.Items["CompanyId"] = companyId.Value;
            }
            else
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Tenant not identified");
                return;
            }

            await _next(context);
        }
    }
}
```

**Регистрация в Program.cs:**

```csharp
// После app.UseAuthentication()
app.UseMiddleware<TenantResolutionMiddleware>();
```

---

### 4. Обновление ContextDB для автоматической фильтрации

```csharp
// APIDeliveryCRM/ContextDb/ContextDB.cs
public class ContextDB : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContextDB(DbContextOptions<ContextDB> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? CurrentCompanyId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.ContainsKey("CompanyId") == true)
            {
                return (int?)httpContext.Items["CompanyId"];
            }
            return null;
        }
    }

    public override int SaveChanges()
    {
        // Автоматически устанавливаем Company_id для новых записей
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (entry.Entity is IHasCompanyId entity && entity.Company_id == 0)
            {
                entity.Company_id = CurrentCompanyId ?? throw new InvalidOperationException("CompanyId not set");
            }
        }

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (entry.Entity is IHasCompanyId entity && entity.Company_id == 0)
            {
                entity.Company_id = CurrentCompanyId ?? throw new InvalidOperationException("CompanyId CompanyId not set");
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ✅ Глобальный фильтр для всех запросов
        if (CurrentCompanyId.HasValue)
        {
            // Применяем фильтр для всех сущностей с Company_id
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IHasCompanyId).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(IHasCompanyId.Company_id));
                    var constant = Expression.Constant(CurrentCompanyId.Value);
                    var filter = Expression.Equal(property, constant);
                    var lambda = Expression.Lambda(filter, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        // ... остальная конфигурация
    }
}

// Интерфейс для сущностей с Company_id
public interface IHasCompanyId
{
    int Company_id { get; set; }
}
```

---

### 5. Обновление сервисов для работы с Tenant

```csharp
// OrderService.cs
public class OrderService : IOrderService
{
    private readonly ContextDB _context;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrderService(ContextDB context, IKafkaProducer kafkaProducer, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _kafkaProducer = kafkaProducer;
        _httpContextAccessor = httpContextAccessor;
    }

    private int CurrentCompanyId
    {
        get
        {
            var companyId = _httpContextAccessor.HttpContext?.Items["CompanyId"];
            if (companyId == null)
                throw new UnauthorizedAccessException("Company not identified");
            return (int)companyId;
        }
    }

    public async Task<Order> CreateAsync(CreateOrderRequest request)
    {
        // Company_id автоматически установится через SaveChanges()
        var order = new Order
        {
            // ... остальные поля
            Company_id = CurrentCompanyId  // ✅ Явно устанавливаем
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // ✅ Публикуем событие с CompanyId
        await _kafkaProducer.ProduceAsync($"orders-events-{CurrentCompanyId}", new
        {
            EventType = "OrderCreated",
            CompanyId = CurrentCompanyId,  // ✅ Добавляем
            OrderId = order.ID_Order,
            OrderNumber = order.Order_Number,
            ClientId = order.Client_id,
            CreatedAt = DateTime.UtcNow
        });

        return order;
    }

    public async Task<IReadOnlyList<Order>> GetByClientAsync(int clientProfileId)
    {
        // ✅ Автоматически фильтруется по Company_id через HasQueryFilter
        return await _context.Orders
            .Where(o => o.Client_id == clientProfileId)
            // Company_id уже применен автоматически!
            .ToListAsync();
    }
}
```

---

## 🔄 Kafka в Multi-Tenant архитектуре

### Вариант 1: Общий Kafka с префиксами топиков (Рекомендуется)

```
Топики:
- orders-events-1  (для компании 1)
- orders-events-2  (для компании 2)
- orders-events-3  (для компании 3)
```

**Преимущества:**
- ✅ Один Kafka кластер
- ✅ Легко масштабировать
- ✅ Изоляция данных

**Реализация:**

```csharp
// KafkaProducerService.cs
public async Task ProduceAsync(string topic, object message)
{
    var companyId = _httpContextAccessor.HttpContext?.Items["CompanyId"];
    var tenantTopic = $"{topic}-{companyId}";  // orders-events-1
    
    var jsonMessage = JsonSerializer.Serialize(message);
    await _producer.ProduceAsync(tenantTopic, new Message<Null, string> { Value = jsonMessage });
}
```

### Вариант 2: Общий топик с CompanyId в сообщении

```
Топик: orders-events (общий для всех)

Сообщение:
{
  "CompanyId": 1,
  "EventType": "OrderCreated",
  "OrderId": 123
}
```

**Преимущества:**
- ✅ Проще управление топиками
- ✅ Легче аналитика

**Недостатки:**
- ⚠️ Нужна фильтрация в Consumer

**Реализация:**

```csharp
// OrderEventConsumer.cs
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _consumer.Subscribe("orders-events");
    
    while (!stoppingToken.IsCancellationRequested)
    {
        var result = _consumer.Consume(stoppingToken);
        var eventData = JsonSerializer.Deserialize<JsonElement>(result.Message.Value);
        
        var companyId = eventData.GetProperty("CompanyId").GetInt32();
        
        // Обрабатываем только для нужной компании
        // (можно запускать отдельный Consumer для каждой компании)
        await ProcessEventAsync(eventData, companyId);
    }
}
```

**Рекомендация:** Вариант 1 (префиксы топиков) для лучшей изоляции.

---

## ☁️ Azure в Multi-Tenant архитектуре

### 1. Azure Blob Storage

#### Вариант 1: Общий Storage Account с префиксами контейнеров

```
Контейнеры:
- deliverycrm-company-1-avatars
- deliverycrm-company-1-reports
- deliverycrm-company-2-avatars
- deliverycrm-company-2-reports
```

**Реализация:**

```csharp
// AzureBlobService.cs
public class AzureBlobService : IAzureBlobService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    private int CurrentCompanyId => (int)_httpContextAccessor.HttpContext!.Items["CompanyId"]!;

    public async Task<string> UploadFileAsync(string blobName, Stream fileStream, string contentType)
    {
        // Используем контейнер компании или общий
        var containerName = _configuration["AzureStorage:ContainerName"] ?? "deliverycrm";
        var tenantContainerName = $"{containerName}-company-{CurrentCompanyId}";
        
        // ... загрузка файла
    }
}
```

#### Вариант 2: Отдельный Storage Account для каждой компании

**Когда использовать:** Для крупных корпоративных клиентов

```csharp
// В модели Company
public string? AzureStorageConnectionString { get; set; }
public string? AzureStorageContainerName { get; set; }

// В AzureBlobService
private BlobContainerClient GetContainerClient()
{
    var company = _context.Companies.Find(CurrentCompanyId);
    
    // Если у компании свой Storage Account
    if (!string.IsNullOrEmpty(company?.AzureStorageConnectionString))
    {
        var blobServiceClient = new BlobServiceClient(company.AzureStorageConnectionString);
        return blobServiceClient.GetBlobContainerClient(company.AzureStorageContainerName ?? "deliverycrm");
    }
    
    // Иначе используем общий
    return _defaultContainerClient;
}
```

**Рекомендация:** Вариант 1 для большинства, Вариант 2 для Enterprise клиентов.

---

### 2. Azure Database for PostgreSQL

#### Вариант 1: Одна БД, фильтрация по Company_id (Рекомендуется)

```
База данных: deliverycrm_production

Таблицы:
- Companies (ID_Company, Name, ...)
- Users (ID_User, Company_id, ...)
- Orders (ID_Order, Company_id, ...)
```

**Преимущества:**
- ✅ Дешевле
- ✅ Проще управление
- ✅ Легче бэкапы

#### Вариант 2: Отдельная БД для каждой компании

**Когда использовать:** Для Enterprise клиентов с особыми требованиями

```
Базы данных:
- deliverycrm_company_1
- deliverycrm_company_2
- deliverycrm_company_3
```

**Реализация:**

```csharp
// ContextDBFactory.cs
public class ContextDBFactory
{
    public static ContextDB Create(int companyId, IConfiguration configuration)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContextDB>();
        
        // Если у компании своя БД
        var company = GetCompany(companyId);
        if (!string.IsNullOrEmpty(company?.DatabaseConnectionString))
        {
            optionsBuilder.UseNpgsql(company.DatabaseConnectionString);
        }
        else
        {
            // Общая БД
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        }
        
        return new ContextDB(optionsBuilder.Options);
    }
}
```

**Рекомендация:** Вариант 1 для большинства случаев.

---

### 3. Azure App Service

#### Вариант 1: Один App Service для всех компаний (Рекомендуется)

```
URL: https://api.deliverycrm.com
Поддомены: dostavka.deliverycrm.com, bystraya.deliverycrm.com
```

**Преимущества:**
- ✅ Дешевле
- ✅ Проще обновления
- ✅ Один код для всех

#### Вариант 2: Отдельный App Service для каждой компании

**Когда использовать:** Для Enterprise клиентов

```
URLs:
- https://dostavka-api.deliverycrm.com
- https://bystraya-api.deliverycrm.com
```

**Рекомендация:** Вариант 1 для большинства случаев.

---

## 🔐 Безопасность в Multi-Tenant

### 1. Изоляция данных

```csharp
// Всегда проверяем Company_id
public async Task<Order> GetByIdAsync(int id)
{
    var order = await _context.Orders
        .FirstOrDefaultAsync(o => o.ID_Order == id);
    
    // ✅ Дополнительная проверка (на случай, если фильтр не сработал)
    if (order?.Company_id != CurrentCompanyId)
    {
        throw new UnauthorizedAccessException("Order not found or access denied");
    }
    
    return order;
}
```

### 2. JWT токен с CompanyId

```csharp
// UserService.cs - Login
public async Task<string> LoginAsync(LoginRequest request)
{
    var login = await _context.Logins
        .Include(l => l.User)
        .FirstOrDefaultAsync(l => l.Email == request.Email);
    
    // Проверяем пароль...
    
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, login.User.ID_User.ToString()),
        new Claim(ClaimTypes.Email, login.Email),
        new Claim(ClaimTypes.Name, $"{login.User.FName} {login.User.Name}"),
        new Claim(ClaimTypes.Role, login.User.Role.Name),
        new Claim("CompanyId", login.User.Company_id.ToString())  // ✅ Добавляем
    };
    
    // ... создание токена
}
```

### 3. Middleware для проверки доступа

```csharp
// APIDeliveryCRM/Middleware/TenantAccessMiddleware.cs
public class TenantAccessMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tokenCompanyId = context.User.FindFirst("CompanyId")?.Value;
            var contextCompanyId = context.Items["CompanyId"]?.ToString();
            
            // ✅ Проверяем, что CompanyId из токена совпадает с CompanyId из контекста
            if (tokenCompanyId != contextCompanyId)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied: Company mismatch");
                return;
            }
        }
        
        await _next(context);
    }
}
```

---

## 📊 Миграция существующих данных

### Шаг 1: Создать миграцию для Company

```bash
dotnet ef migrations add AddCompanyAndTenantId
```

### Шаг 2: Создать дефолтную компанию

```sql
-- В миграции или отдельном скрипте
INSERT INTO "Companies" ("Name", "Subdomain", "Created_at", "Is_Active", "SubscriptionPlan", "MaxUsers", "MaxOrdersPerMonth", "SubscriptionExpiresAt")
VALUES ('Default Company', 'default', NOW(), true, 'Pro', 100, 10000, NOW() + INTERVAL '1 year');

-- Получить ID созданной компании
-- Предположим, это ID = 1
```

### Шаг 3: Обновить существующие записи

```sql
-- Добавить Company_id = 1 ко всем существующим записям
UPDATE "Users" SET "Company_id" = 1 WHERE "Company_id" IS NULL;
UPDATE "Orders" SET "Company_id" = 1 WHERE "Company_id" IS NULL;
-- И так далее для всех таблиц
```

---

## 🚀 Регистрация новой компании

### API Endpoint для регистрации компании

```csharp
// CompaniesController.cs
[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterCompany(RegisterCompanyRequest request)
    {
        // Проверяем, что subdomain уникален
        var existing = await _context.Companies
            .FirstOrDefaultAsync(c => c.Subdomain == request.Subdomain);
        
        if (existing != null)
        {
            return BadRequest(new { message = "Subdomain already taken" });
        }
        
        var company = new Company
        {
            Name = request.Name,
            Subdomain = request.Subdomain,
            Created_at = DateTime.UtcNow,
            Is_Active = true,
            SubscriptionPlan = request.SubscriptionPlan ?? "Basic",
            MaxUsers = request.MaxUsers ?? 10,
            MaxOrdersPerMonth = request.MaxOrdersPerMonth ?? 1000,
            SubscriptionExpiresAt = DateTime.UtcNow.AddMonths(1)
        };
        
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        
        // Создаем первого администратора
        var adminUser = new User
        {
            Company_id = company.ID_Company,
            FName = request.AdminFirstName,
            Name = request.AdminLastName,
            Role_id = 1, // Admin role
            Created_at = DateTime.UtcNow,
            Is_Active = true
        };
        
        // ... создание логина и т.д.
        
        return Ok(new { companyId = company.ID_Company, message = "Company registered successfully" });
    }
}
```

---

## 📈 Масштабирование

### Горизонтальное масштабирование

```
┌─────────────────────────────────────────┐
│         Load Balancer                   │
└──────────────┬──────────────────────────┘
               │
    ┌──────────┼──────────┐
    │          │          │
┌───▼───┐ ┌───▼───┐ ┌───▼───┐
│ API 1 │ │ API 2 │ │ API 3 │
└───┬───┘ └───┬───┘ └───┬───┘
    │          │          │
    └──────────┼──────────┘
               │
    ┌──────────▼──────────┐
    │   PostgreSQL (БД)    │
    └──────────────────────┘
```

### Вертикальное масштабирование

- Увеличить размер БД
- Увеличить размер App Service
- Добавить больше Consumers для Kafka

---

## 💰 Монетизация

### Тарифные планы

```csharp
public class SubscriptionPlan
{
    public const string Basic = "Basic";      // $29/месяц - 10 пользователей, 1000 заказов
    public const string Pro = "Pro";          // $99/месяц - 50 пользователей, 10000 заказов
    public const string Enterprise = "Enterprise"; // $299/месяц - Безлимит, свой Storage
}
```

### Проверка лимитов

```csharp
// OrderService.cs
public async Task<Order> CreateAsync(CreateOrderRequest request)
{
    var company = await _context.Companies.FindAsync(CurrentCompanyId);
    
    // Проверяем лимит заказов
    var ordersThisMonth = await _context.Orders
        .Where(o => o.Company_id == CurrentCompanyId 
            && o.Created_at.Month == DateTime.UtcNow.Month
            && o.Created_at.Year == DateTime.UtcNow.Year)
        .CountAsync();
    
    if (ordersThisMonth >= company.MaxOrdersPerMonth)
    {
        throw new InvalidOperationException("Monthly order limit reached. Please upgrade your plan.");
    }
    
    // ... создание заказа
}
```

---

## ✅ Итоговая архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                    Multi-Tenant SaaS                        │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ Компания 1   │  │ Компания 2   │  │ Компания 3   │       │
│  │ dostavka.com │  │ bystraya.com │  │ express.com  │       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
│         │                 │                  │               │
│         └─────────────────┼──────────────────┘               │
│                           │                                  │
│                  ┌────────▼─────────┐                        │
│                  │  API (App Service)│                       │
│                  │  Tenant Resolution│                       │
│                  └────────┬─────────┘                        │
│                           │                                  │
│         ┌─────────────────┼─────────────────┐               │
│         │                  │                  │               │
│  ┌──────▼──────┐  ┌────────▼────────┐  ┌─────▼──────┐      │
│  │ PostgreSQL  │  │ Azure Blob      │  │ Kafka      │      │
│  │ (Shared DB) │  │ (Shared/Per Co) │  │ (Prefixed) │      │
│  └─────────────┘  └─────────────────┘  └────────────┘      │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 Преимущества Multi-Tenant SaaS

1. ✅ **Одно обновление** - обновляете один раз, все компании получают обновление
2. ✅ **Дешевле** - одна инфраструктура для всех
3. ✅ **Масштабируемость** - легко добавлять новых клиентов
4. ✅ **Проще поддержка** - один код, одна БД
5. ✅ **Монетизация** - подписки, тарифные планы

---

## 📝 Чек-лист для реализации

- [ ] Создать модель Company
- [ ] Добавить Company_id во все модели
- [ ] Создать TenantResolutionMiddleware
- [ ] Обновить ContextDB с HasQueryFilter
- [ ] Обновить JWT токен (добавить CompanyId)
- [ ] Обновить все сервисы для работы с Tenant
- [ ] Настроить Kafka с префиксами топиков
- [ ] Настроить Azure Blob Storage с префиксами
- [ ] Создать API для регистрации компаний
- [ ] Добавить проверку лимитов подписки
- [ ] Настроить мониторинг по компаниям

---

Это полная архитектура для SaaS решения! 🚀


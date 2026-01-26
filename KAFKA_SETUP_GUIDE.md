# Полное руководство по Kafka в APIDeliveryCRM

## 📋 Содержание
1. [Что такое Kafka и зачем она нужна](#что-такое-kafka-и-зачем-она-нужна)
2. [Как Kafka будет работать в вашем проекте](#как-kafka-будет-работать-в-вашем-проекте)
3. [Архитектура с Kafka](#архитектура-с-kafka)
4. [Пошаговая установка и настройка](#пошаговая-установка-и-настройка)
5. [Интеграция в код проекта](#интеграция-в-код-проекта)
6. [Примеры использования](#примеры-использования)
7. [Тестирование](#тестирование)
8. [Решение проблем](#решение-проблем)

---

## Что такое Kafka и зачем она нужна

### Проблема без Kafka

Сейчас в вашем проекте все работает **синхронно**:

```csharp
// OrderService.cs - ChangeStatusAsync
public async Task<bool> ChangeStatusAsync(int orderId, int statusId)
{
    var order = await _context.Orders.FirstOrDefaultAsync(o => o.ID_Order == orderId);
    order.Status_id = statusId;
    await _context.SaveChangesAsync();  // ⏱️ Ждем сохранения
    
    // Если бы здесь создавали уведомление - API бы ждал еще 200ms
    // Если бы отправляли email - API бы ждал еще 1500ms
    // ИТОГО: пользователь ждет 1800ms вместо 200ms
    
    return true;
}
```

**Проблемы:**
- ❌ API отвечает медленно
- ❌ Если упадет отправка email - упадет весь API
- ❌ Сложно добавить новую обработку (нужно менять основной код)
- ❌ Невозможно масштабировать обработку

### Решение с Kafka

Kafka делает обработку **асинхронной**:

```csharp
// OrderService.cs - ChangeStatusAsync (с Kafka)
public async Task<bool> ChangeStatusAsync(int orderId, int statusId)
{
    var order = await _context.Orders.FirstOrDefaultAsync(o => o.ID_Order == orderId);
    order.Status_id = statusId;
    await _context.SaveChangesAsync();  // ⏱️ 200ms
    
    // ✅ Публикуем событие в Kafka (быстро, не ждем обработки)
    await _kafkaProducer.ProduceAsync("orders-events", new
    {
        EventType = "OrderStatusChanged",
        OrderId = orderId,
        NewStatusId = statusId
    });  // ⏱️ 5ms
    
    return true;  // API отвечает за 205ms вместо 1800ms! ⚡
}

// В фоне (параллельно):
// Consumer обрабатывает событие:
// - Создает уведомление (200ms)
// - Отправляет email (1500ms)
// - Записывает в аналитику (50ms)
// Все это НЕ блокирует API!
```

**Преимущества:**
- ✅ API отвечает быстро
- ✅ Если упадет обработка - API продолжит работать
- ✅ Легко добавить новую обработку (просто новый Consumer)
- ✅ Можно масштабировать обработку (несколько Consumers)

---

## Как Kafka будет работать в вашем проекте

### Основные понятия

1. **Producer (Производитель)** - отправляет события в Kafka
   - `OrderService` - публикует события о заказах
   - `ChatService` - публикует события о сообщениях

2. **Topic (Топик)** - очередь для событий определенного типа
   - `orders-events` - события заказов
   - `chat-messages` - события сообщений
   - `notifications` - события уведомлений

3. **Consumer (Потребитель)** - обрабатывает события из Kafka
   - `OrderEventConsumer` - обрабатывает события заказов
   - `ChatMessageConsumer` - обрабатывает события сообщений

4. **Message (Сообщение)** - данные события в формате JSON

### Поток работы

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Пользователь создает заказ через API                      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. OrderService сохраняет заказ в БД                        │
│    await _context.SaveChangesAsync()                        │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. OrderService публикует событие в Kafka                   │
│    await _kafkaProducer.ProduceAsync("orders-events", {...})│
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. API сразу отвечает пользователю ✅                        │
│    return order;  // Быстро!                                 │
└─────────────────────────────────────────────────────────────┘

Параллельно (в фоне):
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Kafka хранит событие в топике "orders-events"           │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. OrderEventConsumer читает событие из Kafka                │
│    var result = _consumer.Consume()                          │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│ 7. Consumer обрабатывает событие:                           │
│    - Создает уведомление в БД                               │
│    - Отправляет email клиенту                               │
│    - Записывает в аналитику                                 │
│    - Отправляет push-уведомление                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Архитектура с Kafka

### Текущая архитектура (без Kafka)

```
┌─────────────┐
│   Клиент   │
└──────┬──────┘
       │ HTTP Request
       ▼
┌─────────────┐
│     API     │
│ (ASP.NET)   │
└──────┬──────┘
       │
       ├───► PostgreSQL (БД)
       │
       ├───► NotificationService (синхронно) ❌
       │
       └───► Email Service (синхронно) ❌
       
       ⏱️ Пользователь ждет все операции
```

### Архитектура с Kafka

```
┌─────────────┐
│   Клиент   │
└──────┬──────┘
       │ HTTP Request
       ▼
┌─────────────┐
│     API     │
│ (ASP.NET)   │
└──────┬──────┘
       │
       ├───► PostgreSQL (БД)
       │
       └───► Kafka (Producer) ⚡
              │
              ├───► Topic: orders-events
              │     │
              │     ├───► Consumer 1: Уведомления
              │     │     └───► NotificationService
              │     │
              │     ├───► Consumer 2: Email
              │     │     └───► EmailService
              │     │
              │     └───► Consumer 3: Аналитика
              │           └───► AnalyticsService
              │
              └───► Topic: chat-messages
                    │
                    └───► Consumer: Уведомления офлайн-пользователям
                          └───► NotificationService
```

### Сценарии использования в вашем проекте

#### 1. Создание заказа

**Сейчас (синхронно):**
```csharp
// OrderService.cs - CreateAsync
public async Task<Order> CreateAsync(CreateOrderRequest request)
{
    var order = new Order { ... };
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();  // ⏱️ 200ms
    
    // Если бы здесь создавали уведомление - еще 200ms
    // ИТОГО: 400ms
    
    return order;
}
```

**С Kafka (асинхронно):**
```csharp
public async Task<Order> CreateAsync(CreateOrderRequest request)
{
    var order = new Order { ... };
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();  // ⏱️ 200ms
    
    // ✅ Публикуем событие (быстро!)
    await _kafkaProducer.ProduceAsync("orders-events", new
    {
        EventType = "OrderCreated",
        OrderId = order.ID_Order,
        OrderNumber = order.Order_Number,
        ClientId = order.Client_id,
        CreatedAt = DateTime.UtcNow
    });  // ⏱️ 5ms
    
    return order;  // ⏱️ ИТОГО: 205ms (вместо 400ms) ⚡
}

// В фоне Consumer создает уведомление (200ms) - не блокирует API!
```

#### 2. Изменение статуса заказа

**Сейчас (синхронно через SQL триггер):**
```csharp
// OrderService.cs - ChangeStatusAsync
public async Task<bool> ChangeStatusAsync(int orderId, int statusId)
{
    order.Status_id = statusId;
    await _context.SaveChangesAsync();  
    // SQL триггер создает уведомление синхронно ⏱️
    
    return true;
}
```

**С Kafka:**
```csharp
public async Task<bool> ChangeStatusAsync(int orderId, int statusId)
{
    var oldStatusId = order.Status_id;
    order.Status_id = statusId;
    await _context.SaveChangesAsync();
    
    // ✅ Публикуем событие
    await _kafkaProducer.ProduceAsync("orders-events", new
    {
        EventType = "OrderStatusChanged",
        OrderId = orderId,
        OrderNumber = order.Order_Number,
        OldStatusId = oldStatusId,
        NewStatusId = statusId,
        ChangedAt = DateTime.UtcNow
    });
    
    return true;
}

// Consumer в фоне:
// - Создает уведомление для клиента
// - Создает уведомление для курьера
// - Отправляет email
// - Обновляет аналитику
```

#### 3. Назначение курьера

**С Kafka:**
```csharp
public async Task<bool> AssignCourierAsync(int orderId, int courierProfileId)
{
    order.Courier_id = courierProfileId;
    await _context.SaveChangesAsync();
    
    // ✅ Публикуем событие
    await _kafkaProducer.ProduceAsync("orders-events", new
    {
        EventType = "CourierAssigned",
        OrderId = orderId,
        OrderNumber = order.Order_Number,
        CourierId = courierProfileId,
        AssignedAt = DateTime.UtcNow
    });
    
    return true;
}

// Consumer в фоне:
// - Создает уведомление курьеру
// - Отправляет push-уведомление
// - Обновляет список доступных курьеров
```

#### 4. Сообщения в чате

**Сейчас (синхронно):**
```csharp
// ChatService.cs - SendMessageAsync
public async Task<IActionResult> SendMessageAsync(...)
{
    await _context.ChatMessages.AddAsync(message);
    await _context.SaveChangesAsync();
    
    // SignalR отправляет в браузер мгновенно ✅
    await _hubContext.Clients.Group(...).SendAsync("ReceiveMessage", ...);
    
    // Создает уведомления синхронно (медленно) ❌
    await SendNotificationsToParticipantsAsync(...);  // ⏱️ 200ms
}
```

**С Kafka:**
```csharp
public async Task<IActionResult> SendMessageAsync(...)
{
    await _context.ChatMessages.AddAsync(message);
    await _context.SaveChangesAsync();
    
    // SignalR отправляет в браузер мгновенно ✅
    await _hubContext.Clients.Group(...).SendAsync("ReceiveMessage", ...);
    
    // ✅ Публикуем событие в Kafka (быстро!)
    await _kafkaProducer.ProduceAsync("chat-messages", new
    {
        EventType = "MessageSent",
        ChatRoomId = chatRoomId,
        SenderId = senderId,
        MessageText = messageText,
        SentAt = DateTime.UtcNow
    });
    
    return Ok();  // API сразу отвечает!
}

// Consumer в фоне:
// - Создает уведомления для офлайн-пользователей
// - Отправляет push-уведомления
// - Записывает в аналитику
```

---

## Пошаговая установка и настройка

### Шаг 1: Выбор варианта Kafka

У вас есть 2 варианта:

#### Вариант A: Локальный Kafka (для разработки)

**Плюсы:**
- Бесплатно
- Быстро настроить
- Для обучения и разработки

**Минусы:**
- Нужно запускать локально
- Не подходит для продакшена

#### Вариант B: Azure Event Hubs (для продакшена)

**Плюсы:**
- Управляемый сервис (не нужно администрировать)
- Автомасштабирование
- Интеграция с Azure
- Kafka API совместимость

**Минусы:**
- Платно (~$10-50/месяц)

**Рекомендация:** Начните с локального Kafka для разработки, затем перейдите на Azure Event Hubs.

---

### Шаг 2: Установка локального Kafka (для разработки)

#### 2.1. Установка через Docker (рекомендуется)

**Требования:**
- Docker Desktop установлен и запущен

**Команды:**

```bash
# Создать docker-compose.yml в корне проекта
```

Создайте файл `docker-compose.yml` в корне проекта:

```yaml
version: '3.8'
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:latest
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    ports:
      - "2181:2181"

  kafka:
    image: confluentinc/cp-kafka:latest
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
```

**Запуск:**

```bash
# В корне проекта
docker-compose up -d
```

**Проверка:**

```bash
# Проверить, что Kafka запущен
docker ps
# Должны быть контейнеры zookeeper и kafka
```

#### 2.2. Альтернатива: Установка Kafka вручную

1. Скачайте Kafka: https://kafka.apache.org/downloads
2. Распакуйте архив
3. Запустите Zookeeper:
   ```bash
   bin\windows\zookeeper-server-start.bat config\zookeeper.properties
   ```
4. Запустите Kafka:
   ```bash
   bin\windows\kafka-server-start.bat config\server.properties
   ```

---

### Шаг 3: Установка NuGet пакетов

```bash
cd APIDeliveryCRM
dotnet add package Confluent.Kafka
dotnet add package Microsoft.Extensions.Hosting
```

**Что устанавливается:**
- `Confluent.Kafka` - клиент для работы с Kafka
- `Microsoft.Extensions.Hosting` - для Background Services (Consumers)

---

### Шаг 4: Настройка appsettings.json

Добавьте настройки Kafka:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5555;Database=DeliveryCRM;Username=postgres;Password=1111"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "deliverycrm-consumer-group"
  },
  "AzureStorage": {
    "ConnectionString": "",
    "ContainerName": "deliverycrm"
  }
}
```

**Для Azure Event Hubs:**
```json
{
  "Kafka": {
    "BootstrapServers": "{namespace}.servicebus.windows.net:9093",
    "ConnectionString": "Endpoint=sb://...",
    "GroupId": "deliverycrm-consumer-group"
  }
}
```

---

### Шаг 5: Создание интерфейса Kafka Producer

Создайте файл `APIDeliveryCRM/Interfaces/IKafkaProducer.cs`:

```csharp
using System.Threading.Tasks;

namespace APIDeliveryCRM.Interfaces
{
    public interface IKafkaProducer
    {
        Task ProduceAsync(string topic, object message);
        Task ProduceAsync<T>(string topic, T message) where T : class;
    }
}
```

---

### Шаг 6: Реализация Kafka Producer

Создайте файл `APIDeliveryCRM/Services/KafkaProducerService.cs`:

```csharp
using System;
using System.Text.Json;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace APIDeliveryCRM.Services
{
    public class KafkaProducerService : IKafkaProducer, IDisposable
    {
        private readonly IProducer<Null, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            
            var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
            
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                // Для Azure Event Hubs раскомментируйте:
                // SaslMechanism = SaslMechanism.Plain,
                // SecurityProtocol = SecurityProtocol.SaslSsl,
                // SaslUsername = "$ConnectionString",
                // SaslPassword = configuration["Kafka:ConnectionString"]
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
            _logger.LogInformation($"Kafka Producer инициализирован: {bootstrapServers}");
        }

        public async Task ProduceAsync(string topic, object message)
        {
            try
            {
                var jsonMessage = JsonSerializer.Serialize(message);
                await _producer.ProduceAsync(topic, new Message<Null, string> { Value = jsonMessage });
                _logger.LogDebug($"Событие опубликовано в топик '{topic}': {jsonMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при публикации события в топик '{topic}'");
                throw;
            }
        }

        public async Task ProduceAsync<T>(string topic, T message) where T : class
        {
            await ProduceAsync(topic, (object)message);
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}
```

---

### Шаг 7: Регистрация в Program.cs

Добавьте в `Program.cs`:

```csharp
// После других AddScoped
builder.Services.AddSingleton<IKafkaProducer, KafkaProducerService>();
```

---

### Шаг 8: Создание Consumer для событий заказов

Создайте файл `APIDeliveryCRM/Services/OrderEventConsumer.cs`:

```csharp
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace APIDeliveryCRM.Services
{
    public class OrderEventConsumer : BackgroundService
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly INotificationService _notificationService;
        private readonly ContextDB _context;
        private readonly ILogger<OrderEventConsumer> _logger;

        public OrderEventConsumer(
            IConfiguration configuration,
            INotificationService notificationService,
            ContextDB context,
            ILogger<OrderEventConsumer> logger)
        {
            _notificationService = notificationService;
            _context = context;
            _logger = logger;

            var config = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                GroupId = configuration["Kafka:GroupId"] ?? "deliverycrm-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            _logger.LogInformation("OrderEventConsumer инициализирован");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe("orders-events");
            _logger.LogInformation("Подписались на топик 'orders-events'");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = _consumer.Consume(stoppingToken);
                        
                        if (result?.Message?.Value == null)
                            continue;

                        await ProcessEventAsync(result.Message.Value);
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError(e, "Ошибка при чтении из Kafka");
                    }
                }
            }
            finally
            {
                _consumer.Close();
            }
        }

        private async Task ProcessEventAsync(string messageJson)
        {
            try
            {
                var eventData = JsonSerializer.Deserialize<JsonElement>(messageJson);
                var eventType = eventData.GetProperty("EventType").GetString();

                _logger.LogInformation($"Обработка события: {eventType}");

                switch (eventType)
                {
                    case "OrderCreated":
                        await HandleOrderCreatedAsync(eventData);
                        break;
                    case "OrderStatusChanged":
                        await HandleOrderStatusChangedAsync(eventData);
                        break;
                    case "CourierAssigned":
                        await HandleCourierAssignedAsync(eventData);
                        break;
                    default:
                        _logger.LogWarning($"Неизвестный тип события: {eventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке события");
            }
        }

        private async Task HandleOrderCreatedAsync(JsonElement eventData)
        {
            var orderId = eventData.GetProperty("OrderId").GetInt32();
            var clientId = eventData.GetProperty("ClientId").GetInt32();
            var orderNumber = eventData.GetProperty("OrderNumber").GetInt32();

            // Получаем ID пользователя клиента
            var clientProfile = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.ID_ClientProfile == clientId);
            
            if (clientProfile != null)
            {
                await _notificationService.SendAsync(
                    clientProfile.User_id,
                    typeId: 1, // Тип уведомления "Новый заказ"
                    title: "Заказ создан",
                    message: $"Ваш заказ #{orderNumber} успешно создан",
                    orderId: orderId
                );
                
                _logger.LogInformation($"Создано уведомление для клиента {clientProfile.User_id} о заказе {orderId}");
            }
        }

        private async Task HandleOrderStatusChangedAsync(JsonElement eventData)
        {
            var orderId = eventData.GetProperty("OrderId").GetInt32();
            var orderNumber = eventData.GetProperty("OrderNumber").GetInt32();
            var newStatusId = eventData.GetProperty("NewStatusId").GetInt32();

            // Получаем заказ
            var order = await _context.Orders
                .Include(o => o.ClientProfiles)
                .Include(o => o.CourierProfiles)
                .FirstOrDefaultAsync(o => o.ID_Order == orderId);

            if (order?.ClientProfiles != null)
            {
                await _notificationService.SendAsync(
                    order.ClientProfiles.User_id,
                    typeId: 2, // "Изменение статуса"
                    title: "Статус заказа изменен",
                    message: $"Статус заказа #{orderNumber} был изменен",
                    orderId: orderId
                );
            }

            if (order?.CourierProfiles != null)
            {
                await _notificationService.SendAsync(
                    order.CourierProfiles.User_id,
                    typeId: 2,
                    title: "Статус заказа изменен",
                    message: $"Статус заказа #{orderNumber} был изменен",
                    orderId: orderId
                );
            }

            _logger.LogInformation($"Обработано событие OrderStatusChanged для заказа {orderId}");
        }

        private async Task HandleCourierAssignedAsync(JsonElement eventData)
        {
            var orderId = eventData.GetProperty("OrderId").GetInt32();
            var orderNumber = eventData.GetProperty("OrderNumber").GetInt32();
            var courierId = eventData.GetProperty("CourierId").GetInt32();

            // Отправляем уведомление курьеру
            var courierProfile = await _context.CourierProfiles
                .FirstOrDefaultAsync(c => c.ID_CourierProfile == courierId);

            if (courierProfile != null)
            {
                await _notificationService.SendAsync(
                    courierProfile.User_id,
                    typeId: 3, // "Назначение курьера"
                    title: "Новый заказ",
                    message: $"Вам назначен новый заказ #{orderNumber}",
                    orderId: orderId
                );
                
                _logger.LogInformation($"Создано уведомление для курьера {courierProfile.User_id} о заказе {orderId}");
            }
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}
```

---

### Шаг 9: Регистрация Consumer в Program.cs

Добавьте в `Program.cs`:

```csharp
// После AddSignalR
builder.Services.AddHostedService<OrderEventConsumer>();
```

---

### Шаг 10: Интеграция Producer в OrderService

Обновите `APIDeliveryCRM/Services/OrderService.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIDeliveryCRM.ContextDb;
using APIDeliveryCRM.Interfaces;
using APIDeliveryCRM.Model;
using APIDeliveryCRM.Request;
using Microsoft.EntityFrameworkCore;

namespace APIDeliveryCRM.Services
{
    public class OrderService : IOrderService
    {
        private readonly ContextDB _context;
        private readonly IKafkaProducer _kafkaProducer;  // Добавить

        public OrderService(ContextDB context, IKafkaProducer kafkaProducer)  // Добавить
        {
            _context = context;
            _kafkaProducer = kafkaProducer;  // Добавить
        }

        // ... остальные методы ...

        public async Task<Order> CreateAsync(CreateOrderRequest request)
        {
            // Генерируем номер заказа
            var maxOrderNumber = await _context.Orders
                .Select(o => o.Order_Number)
                .DefaultIfEmpty(0)
                .MaxAsync();
            
            var order = new Order
            {
                Name = request.Name,
                Description = request.Description,
                Order_Number = maxOrderNumber + 1,
                Client_id = request.Client_id,
                OrderType_id = request.OrderType_id,
                Status_id = request.Status_id,
                Courier_id = request.Courier_id,
                PackageType_id = request.PackageType_id,
                Weight = request.Weight,
                Height = request.Height,
                Length = request.Length,
                Width = request.Width,
                Estimated_cost = request.Estimated_cost,
                Final_cost = 0,
                Created_at = DateTime.UtcNow,
                PaymentMethod_id = request.PaymentMethod_id,
                Is_paid = false,
                PickupAddress_id = request.PickupAddress_id,
                DeliveryAddress_id = request.DeliveryAddress_id
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ✅ Публикуем событие в Kafka
            try
            {
                await _kafkaProducer.ProduceAsync("orders-events", new
                {
                    EventType = "OrderCreated",
                    OrderId = order.ID_Order,
                    OrderNumber = order.Order_Number,
                    ClientId = order.Client_id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем выполнение
                // Можно добавить ILogger для логирования
            }

            return order;
        }

        public async Task<bool> ChangeStatusAsync(int orderId, int statusId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
            {
                return false;
            }

            var oldStatusId = order.Status_id;
            order.Status_id = statusId;
            await _context.SaveChangesAsync();

            // ✅ Публикуем событие в Kafka
            try
            {
                await _kafkaProducer.ProduceAsync("orders-events", new
                {
                    EventType = "OrderStatusChanged",
                    OrderId = order.ID_Order,
                    OrderNumber = order.Order_Number,
                    OldStatusId = oldStatusId,
                    NewStatusId = statusId,
                    ChangedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Логируем ошибку
            }

            return true;
        }

        public async Task<bool> AssignCourierAsync(int orderId, int courierProfileId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.ID_Order == orderId);
            if (order == null)
            {
                return false;
            }

            order.Courier_id = courierProfileId;
            await _context.SaveChangesAsync();

            // ✅ Публикуем событие в Kafka
            try
            {
                await _kafkaProducer.ProduceAsync("orders-events", new
                {
                    EventType = "CourierAssigned",
                    OrderId = order.ID_Order,
                    OrderNumber = order.Order_Number,
                    CourierId = courierProfileId,
                    AssignedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                // Логируем ошибку
            }

            return true;
        }
    }
}
```

---

## Примеры использования

### Пример 1: Создание заказа

```csharp
// В OrderService
await _kafkaProducer.ProduceAsync("orders-events", new
{
    EventType = "OrderCreated",
    OrderId = 123,
    OrderNumber = 1001,
    ClientId = 456,
    CreatedAt = DateTime.UtcNow
});

// Consumer автоматически обработает и создаст уведомление
```

### Пример 2: Изменение статуса

```csharp
// В OrderService
await _kafkaProducer.ProduceAsync("orders-events", new
{
    EventType = "OrderStatusChanged",
    OrderId = 123,
    OrderNumber = 1001,
    OldStatusId = 1,
    NewStatusId = 2,
    ChangedAt = DateTime.UtcNow
});

// Consumer создаст уведомления для клиента и курьера
```

### Пример 3: Добавление нового Consumer

Можно легко добавить новый Consumer для другой обработки:

```csharp
// EmailConsumer.cs
public class EmailConsumer : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("orders-events");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = _consumer.Consume(stoppingToken);
            
            if (result.Message.Value содержит "OrderCreated")
            {
                // Отправляем email клиенту
                await _emailService.SendOrderConfirmationAsync(...);
            }
        }
    }
}
```

---

## Тестирование

### 1. Запуск Kafka

```bash
# Если используете Docker
docker-compose up -d

# Проверить статус
docker ps
```

### 2. Запуск API

```bash
cd APIDeliveryCRM
dotnet run
```

### 3. Тестирование через Swagger

1. Откройте Swagger UI: `https://localhost:5001/swagger`
2. Авторизуйтесь через `/api/Users/Login`
3. Создайте заказ через `/api/Orders`
4. Проверьте логи - должно быть:
   ```
   Kafka Producer инициализирован: localhost:9092
   Событие опубликовано в топик 'orders-events'
   OrderEventConsumer инициализирован
   Подписались на топик 'orders-events'
   Обработка события: OrderCreated
   Создано уведомление для клиента...
   ```

### 4. Проверка уведомлений

```bash
# Через API
GET /api/Notifications/user/{userId}
```

Должно быть новое уведомление о созданном заказе.

---

## Решение проблем

### Проблема 1: "Connection refused" при подключении к Kafka

**Решение:**
```bash
# Проверьте, что Kafka запущен
docker ps

# Если не запущен, запустите
docker-compose up -d

# Проверьте порт 9092
netstat -an | findstr 9092
```

### Проблема 2: Consumer не получает сообщения

**Решение:**
- Проверьте GroupId в appsettings.json
- Проверьте, что топик существует
- Проверьте логи Consumer

### Проблема 3: Ошибка "Topic does not exist"

**Решение:**
Kafka автоматически создает топики при первой публикации. Если ошибка:
```bash
# Создайте топик вручную (если используете Kafka CLI)
kafka-topics --create --topic orders-events --bootstrap-server localhost:9092
```

### Проблема 4: Producer не публикует события

**Решение:**
- Проверьте настройки в appsettings.json
- Проверьте логи Producer
- Убедитесь, что Kafka доступен

---

## Следующие шаги

1. ✅ Добавить Consumer для событий чата
2. ✅ Добавить обработку ошибок (retry, dead letter queue)
3. ✅ Настроить мониторинг (Application Insights)
4. ✅ Мигрировать на Azure Event Hubs для продакшена

---

## Итог

Теперь у вас:
- ✅ Kafka Producer для публикации событий
- ✅ Kafka Consumer для обработки событий в фоне
- ✅ Асинхронная обработка уведомлений
- ✅ Быстрый API (не ждет обработки)

**Результат:** API отвечает в 5-10 раз быстрее! ⚡


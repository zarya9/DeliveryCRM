# Практические примеры: Kafka и Azure в APIDeliveryCRM

## 📋 Содержание
1. [Настройка Kafka Producer](#1-настройка-kafka-producer)
2. [Интеграция с Azure Blob Storage](#2-интеграция-с-azure-blob-storage)
3. [Создание Kafka Consumer](#3-создание-kafka-consumer)
4. [Настройка Azure Event Hubs](#4-настройка-azure-event-hubs)

---

## 1. Настройка Kafka Producer

### Шаг 1: Установка пакетов

```bash
dotnet add package Confluent.Kafka
dotnet add package Microsoft.Extensions.Hosting
```

### Шаг 2: Создание интерфейса для Producer

**`APIDeliveryCRM/Interfaces/IKafkaProducer.cs`**
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

### Шаг 3: Реализация Producer

**`APIDeliveryCRM/Services/KafkaProducerService.cs`**
```csharp
using System;
using System.Text.Json;
using System.Threading.Tasks;
using APIDeliveryCRM.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace APIDeliveryCRM.Services
{
    public class KafkaProducerService : IKafkaProducer, IDisposable
    {
        private readonly IProducer<Null, string> _producer;
        private readonly IConfiguration _configuration;

        public KafkaProducerService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            var config = new ProducerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                // Для Azure Event Hubs используйте:
                // BootstrapServers = "{your-namespace}.servicebus.windows.net:9093",
                // SaslMechanism = SaslMechanism.Plain,
                // SecurityProtocol = SecurityProtocol.SaslSsl,
                // SaslUsername = "$ConnectionString",
                // SaslPassword = "{your-connection-string}"
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task ProduceAsync(string topic, object message)
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            await _producer.ProduceAsync(topic, new Message<Null, string> { Value = jsonMessage });
        }

        public async Task ProduceAsync<T>(string topic, T message) where T : class
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            await _producer.ProduceAsync(topic, new Message<Null, string> { Value = jsonMessage });
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}
```

### Шаг 4: Обновление OrderService для публикации событий

**Обновление `APIDeliveryCRM/Services/OrderService.cs`**
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
        private readonly IKafkaProducer _kafkaProducer; // Добавить

        public OrderService(ContextDB context, IKafkaProducer kafkaProducer) // Добавить
        {
            _context = context;
            _kafkaProducer = kafkaProducer; // Добавить
        }

        // ... существующие методы ...

        public async Task<Order> CreateAsync(CreateOrderRequest request)
        {
            // ... существующий код создания заказа ...

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Публикуем событие в Kafka
            await _kafkaProducer.ProduceAsync("orders-events", new
            {
                EventType = "OrderCreated",
                OrderId = order.ID_Order,
                OrderNumber = order.Order_Number,
                ClientId = order.Client_id,
                StatusId = order.Status_id,
                CreatedAt = order.Created_at
            });

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

            // Публикуем событие изменения статуса
            await _kafkaProducer.ProduceAsync("orders-events", new
            {
                EventType = "OrderStatusChanged",
                OrderId = order.ID_Order,
                OrderNumber = order.Order_Number,
                OldStatusId = oldStatusId,
                NewStatusId = statusId,
                ChangedAt = DateTime.UtcNow
            });

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

            // Публикуем событие назначения курьера
            await _kafkaProducer.ProduceAsync("orders-events", new
            {
                EventType = "CourierAssigned",
                OrderId = order.ID_Order,
                OrderNumber = order.Order_Number,
                CourierId = courierProfileId,
                AssignedAt = DateTime.UtcNow
            });

            return true;
        }
    }
}
```

### Шаг 5: Регистрация в Program.cs

```csharp
// Добавить в Program.cs
builder.Services.AddSingleton<IKafkaProducer, KafkaProducerService>();
```

### Шаг 6: Настройка appsettings.json

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092"
    // Для Azure Event Hubs:
    // "BootstrapServers": "{namespace}.servicebus.windows.net:9093",
    // "ConnectionString": "{your-connection-string}"
  }
}
```

---

## 2. Интеграция с Azure Blob Storage

### Шаг 1: Установка пакетов

```bash
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Identity
```

### Шаг 2: Обновление интерфейса FileService

**`APIDeliveryCRM/Interfaces/IFileService.cs`** (добавить методы)
```csharp
// Существующие методы + новые для Azure Blob
Task<string> UploadAvatarToAzureAsync(int userId, IFormFile file);
Task<string> UploadReportToAzureAsync(int userId, string reportType, IFormFile file);
Task<Stream> DownloadFileFromAzureAsync(string blobName);
```

### Шаг 3: Реализация Azure Blob Storage

**`APIDeliveryCRM/Services/AzureBlobService.cs`** (новый файл)
```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace APIDeliveryCRM.Services
{
    public class AzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public AzureBlobService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _containerName = configuration["AzureStorage:ContainerName"] ?? "deliverycrm";
            
            _blobServiceClient = new BlobServiceClient(connectionString);
            
            // Создаем контейнер, если его нет
            InitializeContainerAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeContainerAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        }

        public async Task<string> UploadFileAsync(string blobName, Stream fileStream, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(fileStream, overwrite: true);
            
            // Устанавливаем content type
            await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders
            {
                ContentType = contentType
            });

            return blobClient.Uri.ToString();
        }

        public async Task<Stream> DownloadFileAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task<bool> DeleteFileAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.DeleteIfExistsAsync();
        }
    }
}
```

### Шаг 4: Обновление FileService

**`APIDeliveryCRM/Services/FileService.cs`** (добавить использование Azure)
```csharp
// В конструктор добавить:
private readonly AzureBlobService _azureBlobService;

public FileService(IConfiguration configuration, IWebHostEnvironment environment, AzureBlobService azureBlobService)
{
    // ... существующий код ...
    _azureBlobService = azureBlobService;
}

// Новый метод для загрузки аватара в Azure
public async Task<string> UploadAvatarToAzureAsync(int userId, IFormFile file)
{
    var extension = Path.GetExtension(file.FileName);
    var blobName = $"avatars/{userId}{extension}";
    
    using var stream = file.OpenReadStream();
    var url = await _azureBlobService.UploadFileAsync(blobName, stream, file.ContentType);
    
    // Обновляем путь в БД
    var user = await _context.Users.FindAsync(userId);
    if (user != null)
    {
        user.Avatar = url;
        await _context.SaveChangesAsync();
    }
    
    return url;
}
```

### Шаг 5: Настройка appsettings.json

```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "ContainerName": "deliverycrm"
  }
}
```

---

## 3. Создание Kafka Consumer

### Шаг 1: Создание Background Service

**`APIDeliveryCRM/Services/OrderEventConsumer.cs`**
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
                GroupId = "order-events-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe("orders-events");

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

            // Получаем ID пользователя клиента
            var clientProfile = await _context.ClientProfiles
                .FirstOrDefaultAsync(c => c.ID_ClientProfile == clientId);
            
            if (clientProfile != null)
            {
                await _notificationService.SendAsync(
                    clientProfile.User_id,
                    typeId: 1, // Тип уведомления "Новый заказ"
                    title: "Заказ создан",
                    message: $"Ваш заказ #{eventData.GetProperty("OrderNumber").GetInt32()} успешно создан"
                );
            }

            _logger.LogInformation($"Обработано событие OrderCreated для заказа {orderId}");
        }

        private async Task HandleOrderStatusChangedAsync(JsonElement eventData)
        {
            var orderId = eventData.GetProperty("OrderId").GetInt32();
            var orderNumber = eventData.GetProperty("OrderNumber").GetInt32();
            var newStatusId = eventData.GetProperty("NewStatusId").GetInt32();

            // Получаем заказ и отправляем уведомления клиенту и курьеру
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
                    message: $"Вам назначен новый заказ #{eventData.GetProperty("OrderNumber").GetInt32()}",
                    orderId: orderId
                );
            }

            _logger.LogInformation($"Обработано событие CourierAssigned для заказа {orderId}");
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}
```

### Шаг 2: Регистрация Consumer в Program.cs

```csharp
// Добавить в Program.cs
builder.Services.AddHostedService<OrderEventConsumer>();
```

---

## 4. Настройка Azure Event Hubs

### Создание Event Hub в Azure Portal

1. Создайте **Event Hubs Namespace**
2. Создайте **Event Hub** с именем `orders-events`
3. Получите **Connection String**

### Обновление KafkaProducerService для Azure

```csharp
var config = new ProducerConfig
{
    BootstrapServers = $"{namespaceName}.servicebus.windows.net:9093",
    SaslMechanism = SaslMechanism.Plain,
    SecurityProtocol = SecurityProtocol.SaslSsl,
    SaslUsername = "$ConnectionString",
    SaslPassword = connectionString
};
```

### Обновление appsettings.json

```json
{
  "Kafka": {
    "BootstrapServers": "{namespace}.servicebus.windows.net:9093",
    "ConnectionString": "Endpoint=sb://..."
  }
}
```

---

## 🧪 Тестирование

### Локальное тестирование с Kafka

1. Установите Kafka локально или используйте Docker:
```bash
docker run -d -p 9092:9092 apache/kafka:latest
```

2. Запустите API
3. Создайте заказ через Swagger
4. Проверьте, что событие опубликовано в Kafka

### Тестирование Azure Blob Storage

1. Создайте Storage Account в Azure
2. Получите Connection String
3. Загрузите аватар через API
4. Проверьте, что файл появился в Blob Storage

---

## 📝 Следующие шаги

1. ✅ Добавить обработку ошибок
2. ✅ Добавить retry логику
3. ✅ Настроить мониторинг через Application Insights
4. ✅ Добавить больше типов событий
5. ✅ Реализовать dead letter queue для неудачных сообщений

---

## 🔗 Полезные ссылки

- [Confluent Kafka .NET](https://github.com/confluentinc/confluent-kafka-dotnet)
- [Azure Event Hubs для Kafka](https://learn.microsoft.com/azure/event-hubs/event-hubs-for-kafka-ecosystem-overview)
- [Azure Blob Storage](https://learn.microsoft.com/azure/storage/blobs/)


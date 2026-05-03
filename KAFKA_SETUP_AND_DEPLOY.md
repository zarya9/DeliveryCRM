# Настройка Kafka для DeliveryCRM (пошагово)

Этот файл описывает:
- запуск Kafka локально для разработки;
- настройку API для локальной и серверной среды;
- порядок публикации проекта на сервер.

## 1) Что должно быть установлено

- Docker Desktop (или Docker Engine + Docker Compose)
- .NET 8 SDK

Порты, которые используются:
- `9092` — Kafka
- `8085` — Kafka UI (локальная панель просмотра топиков)

## 2) Запуск Kafka локально

Открой терминал в корне проекта `DeliveryCRM` и выполни:

```powershell
docker compose up -d
```

Поднимутся контейнеры:
- `zookeeper`
- `kafka`
- `kafka-ui` (`http://localhost:8085`)
- `kafka-init-topics` (создаст топики `orders-events` и `orders-events-dev`)

Проверка, что все запустилось:

```powershell
docker compose ps
```

Если нужно посмотреть ошибки Kafka:

```powershell
docker compose logs kafka --tail=100
```

## 3) Настройки Kafka в API

Файлы с настройками:
- `APIDeliveryCRM/appsettings.json`
- `APIDeliveryCRM/appsettings.Development.json`
- `APIDeliveryCRM/appsettings.Production.json`

Главный блок:

```json
"Kafka": {
  "BootstrapServers": "localhost:9092",
  "ClientId": "apideliverycrm-api-dev",
  "SecurityProtocol": "Plaintext",
  "SaslMechanism": "",
  "Username": "",
  "Password": "",
  "OrderEventsTopic": "orders-events-dev"
}
```

Для локалки:
- `SecurityProtocol = Plaintext`
- `Username` и `Password` пустые

Для продакшена:
- `SecurityProtocol = SaslSsl`
- `SaslMechanism = Plain` (или нужный для вашего кластера)
- заполнить `Username` и `Password`

## 4) Запуск API локально

```powershell
dotnet run --project APIDeliveryCRM/APIDeliveryCRM.csproj
```

После создания/обновления заказа API публикует событие в топик:
- `Kafka:OrderEventsTopic`

## 5) Публикация проекта на сервер (порядок)

1. Подготовить сервер:
   - установить Docker и Docker Compose;
   - настроить firewall (не открывать Kafka в интернет без необходимости).

2. Выбрать, где будет Kafka:
   - своя Kafka на сервере/VPS;
   - или managed Kafka (Confluent Cloud, Aiven и т.д.).

3. Передать прод-настройки через переменные окружения:

```powershell
KAFKA__BOOTSTRAPSERVERS=<broker:port>
KAFKA__SECURITYPROTOCOL=SaslSsl
KAFKA__SASLMECHANISM=Plain
KAFKA__USERNAME=<username>
KAFKA__PASSWORD=<password>
KAFKA__ORDEREVENTSTOPIC=orders-events
```

4. Собрать приложение:

```powershell
dotnet publish APIDeliveryCRM/APIDeliveryCRM.csproj -c Release -o .\publish\api
```

5. Запустить приложение в режиме Production:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet .\publish\api\APIDeliveryCRM.dll
```

## 6) Что обязательно для продакшена

- Использовать `SaslSsl` (безопасное подключение).
- Не хранить логин/пароль Kafka в git.
- Ограничить доступ к Kafka по сети.
- Держать стабильное имя топика (`orders-events`).
- Подключить мониторинг:
  - лаг consumers;
  - ошибки публикации;
  - состояние брокера.

## 7) Быстрая диагностика

- Ошибка: `Kafka producer disabled: Kafka:BootstrapServers is empty`  
  Решение: заполнить `Kafka:BootstrapServers`.

- Ошибка: `Kafka publish failed for topic ...`  
  Решение: проверить доступность брокера, логин/пароль, имя топика.

- Сообщения не появляются в топике  
  Решение: проверить, что в `OrderService` реально вызывается публикация события.

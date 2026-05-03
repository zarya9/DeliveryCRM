# Приложение: программный код проекта DeliveryCRM

Документ добавлен для закрытия замечания "нет приложения с программным кодом приложения".

Ниже приведен перечень ключевых файлов исходного кода, которые рекомендуется включить в приложение ВКР (полностью или фрагментами).

## 1) Backend (ASP.NET Core)

- `APIDeliveryCRM/Program.cs` - конфигурация приложения, DI, auth, CORS, маршрутизация.
- `APIDeliveryCRM/ContextDb/ContextDB.cs` - модель данных EF Core и связи сущностей.
- `APIDeliveryCRM/Controllers/OrdersController.cs` - API заказов.
- `APIDeliveryCRM/Services/OrderService.cs` - бизнес-логика заказов, SLA, назначение курьеров.
- `APIDeliveryCRM/Controllers/ChatController.cs` - API чата.
- `APIDeliveryCRM/Hubs/ChatHub.cs` - SignalR-хаб.
- `APIDeliveryCRM/Controllers/BillingController.cs` - API биллинга.
- `APIDeliveryCRM/Services/BillingService.cs` - логика подписок/инвойсов/платежей.
- `APIDeliveryCRM/Controllers/ReportsController.cs` - API аналитики и отчетов.
- `APIDeliveryCRM/Controllers/GeoAnalyticsController.cs` - API геоаналитики.

## 2) Frontend (Blazor Server)

- `WebBlazorDeliveryCRM/Program.cs` - конфигурация клиента и DI.
- `WebBlazorDeliveryCRM/Components/Routes.razor` - маршрутизация страниц.
- `WebBlazorDeliveryCRM/Components/Pages/Login.razor` - авторизация и role-redirect.
- `WebBlazorDeliveryCRM/Components/Pages/Manager/Analytics.razor` - аналитика менеджера.
- `WebBlazorDeliveryCRM/Components/Pages/Manager/GeoAnalytics.razor` - геоаналитика.
- `WebBlazorDeliveryCRM/Components/Pages/Logistician/Distribution.razor` - распределение заказов.
- `WebBlazorDeliveryCRM/Components/Pages/Logistician/RouteOptimization.razor` - маршрутизация.
- `WebBlazorDeliveryCRM/Components/Shared/CompanyChatPanel.razor` - корпоративный чат.

## 3) Клиентские сервисы и realtime

- `WebBlazorDeliveryCRM/Services/OrdersApiService.cs`
- `WebBlazorDeliveryCRM/Services/ChatApiService.cs`
- `WebBlazorDeliveryCRM/Services/ChatHubClientService.cs`
- `WebBlazorDeliveryCRM/Services/BillingApiService.cs`

## 4) SQL и seed-материалы

- `APIDeliveryCRM/Database/SeedLargeValidData.sql` - справочники и независимые seed-данные.
- `APIDeliveryCRM/Database/BusinessLogic.sql` - SQL-логика (при необходимости в приложении).

## 5) Рекомендация по оформлению приложения

1. Включать не весь код подряд, а смысловые фрагменты:
   - конфигурация безопасности;
   - ключевые бизнес-методы;
   - примеры API-endpoints;
   - примеры UI страниц.
2. Для каждого фрагмента указывать:
   - "Листинг X.Y";
   - путь к файлу;
   - краткое пояснение назначения (1-2 предложения).
3. Сохранять единый стиль форматирования и нумерации листингов.

# Chat Memory Handoff (Full Project Context)

Этот файл предназначен для полного восстановления контекста после переустановки ОС/потери истории чатов.
Здесь собраны: архитектура, реализованные блоки, ключевые файлы, последние изменения, риски и roadmap.

## 1) Проект и стек

- Repository root: `D:/Zarya/APIDeliveryCRM`
- Backend: `APIDeliveryCRM` (ASP.NET Core 8, EF Core, PostgreSQL, SignalR, JWT)
- Frontend: `WebBlazorDeliveryCRM` (Blazor Server, Bootstrap/MudBlazor, SignalR client)
- Базовая архитектура:
  - Controllers -> Services -> EF Context
  - DTO/Request/Response слои
  - DI в `Program.cs`

## 2) История и цель разработки

Изначально работа шла по диплому/аналитической части, потом фокус полностью сместился на разработку CRM и внедрение большого списка бизнес-фич:
автодиспетчеризация, SLA-контроль, лиды, тикеты, уведомления, геозоны, автопарк, billing (включая YooKassa), шаблоны коммуникаций, плановые отчеты, финальная UI/UX полировка.

## 3) Реализованные функциональные блоки (детально)

### 3.1 Заказы: автодиспетчеризация, SLA, ETA, timeline

- Добавлены поля SLA/ETA/приоритета и временные метки этапов в `Order`.
- Добавлена сущность событий `OrderTimelineEvent`.
- Добавлены API:
  - автоназначение курьера,
  - ручной override курьера,
  - timeline заказа,
  - ETA/SLA статус.
- Реализована логика scoring (distance/load/priority) + SLA risk/breach.
- Добавлены уведомления менеджерам/логистам по SLA.
- Основные файлы:
  - `APIDeliveryCRM/Model/Order.cs`
  - `APIDeliveryCRM/Model/OrderTimelineEvent.cs`
  - `APIDeliveryCRM/Services/OrderService.cs`
  - `APIDeliveryCRM/Controllers/OrdersController.cs`
  - `WebBlazorDeliveryCRM/Services/OrdersApiService.cs`
  - `WebBlazorDeliveryCRM/Models/OrderDto.cs`

### 3.2 Поддержка (tickets)

- Полноценный блок тикетов: категории, статусы, SLA, назначение ответственного, аналитика.
- API: list/create/assign/status-change/analytics.
- Основные файлы:
  - `APIDeliveryCRM/Model/SupportTicket*.cs`
  - `APIDeliveryCRM/Services/SupportTicketService.cs`
  - `APIDeliveryCRM/Controllers/SupportTicketsController.cs`
  - `WebBlazorDeliveryCRM/Services/SupportTicketsApiService.cs`

### 3.3 Leads 2.0

- Победа/потеря лидов, причины потерь, next-task поля, аналитика/funnel.
- API для won/lost + analytics.
- Основные файлы:
  - `APIDeliveryCRM/Model/Lead.cs`
  - `APIDeliveryCRM/Services/LeadService.cs`
  - `APIDeliveryCRM/Controllers/LeadsController.cs`
  - `WebBlazorDeliveryCRM/Services/LeadsApiService.cs`

### 3.4 Notification Center

- Уведомления с priority/critical/requires_ack/acknowledged_at.
- Фильтры и acknowledge endpoint.
- UI фильтры + кнопка подтверждения.
- Основные файлы:
  - `APIDeliveryCRM/Model/Notification.cs`
  - `APIDeliveryCRM/Services/NotificationService.cs`
  - `APIDeliveryCRM/Controllers/NotificationsController.cs`
  - `WebBlazorDeliveryCRM/Components/Pages/Manager/Notifications.razor`
  - `WebBlazorDeliveryCRM/Services/NotificationsApiService.cs`

### 3.5 Геозоны и ограничения назначения

- ServiceAreaZone + mapping с курьерами.
- Запрет назначения курьера вне зоны (если активные зоны есть).
- Основные файлы:
  - `APIDeliveryCRM/Model/ServiceAreaZone*.cs`
  - `APIDeliveryCRM/Services/ServiceAreaZoneService.cs`
  - `APIDeliveryCRM/Controllers/ServiceAreaZonesController.cs`
  - `APIDeliveryCRM/Services/OrderService.cs` (интеграция проверки)

### 3.6 Автопарк и документы

- Поля `Vehicle`: insurance/registration/maintenance due + availability.
- Валидация назначения ТС курьеру с учетом истечения документов/доступности.
- Endpoint expiring docs.
- Основные файлы:
  - `APIDeliveryCRM/Model/Vehicle.cs`
  - `APIDeliveryCRM/Controllers/VehiclesController.cs`
  - `APIDeliveryCRM/Services/CourierService.cs`

### 3.7 Billing (MVP + production hardening)

- Реализованы:
  - тарифные планы,
  - подписка компании,
  - инвойсы,
  - транзакции,
  - checkout,
  - webhook mock + YooKassa webhook.
- Добавлена идемпотентность webhook через `BillingWebhookEvent`.
- Добавлена security-проверка webhook (secret/IP) и production guard:
  - в `Production` webhook отклоняется, если `WebhookSecret` не задан.
- Бизнес-ограничения:
  - запрет создания заказа при просроченной подписке,
  - лимит заказов/месяц по плану.
- Основные файлы:
  - `APIDeliveryCRM/Services/BillingService.cs`
  - `APIDeliveryCRM/Controllers/BillingController.cs`
  - `APIDeliveryCRM/Model/Billing*.cs`
  - `WebBlazorDeliveryCRM/Components/Pages/Manager/Billing.razor`
  - `WebBlazorDeliveryCRM/Services/BillingApiService.cs`

### 3.8 Коммуникационные шаблоны

- Шаблоны текстов для авто-сообщений при смене статусов заказа.
- Placeholders и рендер шаблонов в сервисе.
- Интеграция в flow изменения статуса заказа.
- Основные файлы:
  - `APIDeliveryCRM/Model/CommunicationTemplate.cs`
  - `APIDeliveryCRM/Services/CommunicationTemplateService.cs`
  - `APIDeliveryCRM/Controllers/CommunicationTemplatesController.cs`
  - `WebBlazorDeliveryCRM/Components/Pages/Manager/CommunicationTemplates.razor`

### 3.9 Плановые отчеты

- Scheduled jobs (daily/weekly/monthly), next-run расчет, run-now.
- Фоновый воркер исполнения.
- UI управления заданиями.
- Основные файлы:
  - `APIDeliveryCRM/Model/ScheduledReportJob.cs`
  - `APIDeliveryCRM/Services/ScheduledReportService.cs`
  - `APIDeliveryCRM/Services/ScheduledReportWorker.cs`
  - `APIDeliveryCRM/Controllers/ScheduledReportsController.cs`
  - `WebBlazorDeliveryCRM/Components/Pages/Manager/ScheduledReports.razor`

## 4) UI/UX полировка (сделано)

- Введены общие компоненты:
  - `WebBlazorDeliveryCRM/Components/Shared/StatusBadge.razor`
  - `WebBlazorDeliveryCRM/Components/Shared/KpiCard.razor`
- Унификация статусов/метрик на ключевых страницах менеджера/логиста.
- Добавлены SLA alert акценты в аналитике.
- Улучшены таблицы (hover/readability, dark-friendly).
- Добавлена custom 404:
  - `WebBlazorDeliveryCRM/Components/Pages/NotFoundView.razor`
  - подключена через `WebBlazorDeliveryCRM/Components/Routes.razor`
  - текст: `МИМО`.
- Добавлена персонализация UI для пользователя (локально в браузере):
  - выбор акцентного цвета,
  - выбор фонового стиля (plain/grid/dots/gradient),
  - настройка скругления карточек.
  - сохранение настроек в `localStorage`.
  - файлы:
    - `WebBlazorDeliveryCRM/wwwroot/js/theme.js`
    - `WebBlazorDeliveryCRM/Components/Layout/AppBar.razor`
    - `WebBlazorDeliveryCRM/wwwroot/app.css`

## 5) Realtime/SignalR улучшения (сделано)

- Индикаторы состояния realtime/presence добавлены на ключевых страницах.
- Периодический авто-refresh добавлен на части логистических экранов.
- Файлы:
  - `WebBlazorDeliveryCRM/Components/Pages/Manager/Notifications.razor`
  - `WebBlazorDeliveryCRM/Components/Pages/Manager/Employees.razor`
  - `WebBlazorDeliveryCRM/Components/Pages/Logistician/CourierManagement.razor`

## 5.1 Chat UX overhaul (Telegram-like)

- Переработан `CompanyChatPanel`:
  - слева список чатов (правее главного nav menu),
  - по умолчанию доступен чат компании,
  - можно начать личный чат с сотрудником; если чата нет — создается автоматически,
  - telegram-like composer:
    - поле сообщения,
    - справа кнопка-иконка отправки (самолетик),
    - слева внутри composer кнопка-скрепка,
    - поддержка прикрепления файла + минимальный image preview.
- Перенесены/добавлены API для списка/создания чатов:
  - `GET /api/Chat/rooms/list`
  - `POST /api/Chat/rooms/company`
  - `POST /api/Chat/rooms/direct?peerUserId=...`
- Ключевые файлы:
  - `APIDeliveryCRM/Services/ChatService.cs`
  - `APIDeliveryCRM/Controllers/ChatController.cs`
  - `APIDeliveryCRM/Interfaces/IChatService.cs`
  - `APIDeliveryCRM/Responses/ChatRoomListItemDto.cs`
  - `WebBlazorDeliveryCRM/Services/ChatApiService.cs`
  - `WebBlazorDeliveryCRM/Components/Shared/CompanyChatPanel.razor`
  - `WebBlazorDeliveryCRM/wwwroot/app.css`

## 5.2 Header/Navigation tweaks

- Иконка уведомлений вынесена в header рядом с выходом (без текста, placeholder icon).
- Пункт "Уведомления" убран из левого manager sidebar.
- Файлы:
  - `WebBlazorDeliveryCRM/Components/Layout/AppBar.razor`
  - `WebBlazorDeliveryCRM/Components/Nav/ManagerSidebar.razor`

## 5.3 Большой модуль геоаналитики (новое)

- Добавлен отдельный backend-модуль геоаналитики:
  - `IGeoAnalyticsService` + `GeoAnalyticsService`
  - `GeoAnalyticsController` (`GET /api/GeoAnalytics/overview`)
  - DTO ответа с KPI и срезами: heat points, зоны, курьеры, почасовой спрос, статусы.
- Новая manager-страница `Геоаналитика`:
  - путь: `/manager/geoanalytics`
  - период, шаг сетки heatmap, переключатели heatmap/зон
  - KPI карточки
  - карта с heatmap + круги зон обслуживания
  - таблицы эффективности по зонам и курьерам
  - почасовой срез спроса.
- Расширен JS-карт модуль:
  - `leafletMap.setCircles(...)` / `leafletMap.clearCircles()`.
- Добавлен пункт меню в sidebar менеджера: `Геоаналитика`.
- Ключевые файлы:
  - `APIDeliveryCRM/Responses/GeoAnalyticsOverviewDto.cs`
  - `APIDeliveryCRM/Interfaces/IGeoAnalyticsService.cs`
  - `APIDeliveryCRM/Services/GeoAnalyticsService.cs`
  - `APIDeliveryCRM/Controllers/GeoAnalyticsController.cs`
  - `WebBlazorDeliveryCRM/Models/GeoAnalyticsDtos.cs`
  - `WebBlazorDeliveryCRM/Services/GeoAnalyticsApiService.cs`
  - `WebBlazorDeliveryCRM/Components/Pages/Manager/GeoAnalytics.razor`
  - `WebBlazorDeliveryCRM/wwwroot/js/leafletMap.js`

## 6) Чат: что было и что добавлено последним

### 6.1 До последних изменений

- Общий чат компании с историей, edit/delete, SignalR receive/edit/delete.
- Основной компонент: `WebBlazorDeliveryCRM/Components/Shared/CompanyChatPanel.razor`.

### 6.2 Последние изменения (уже внедрены)

- Быстрые шаблоны ответа над textbox (quick replies).
- Вложения файлов к сообщениям:
  - Upload через `InputFile`,
  - API upload вложения,
  - отправка `attachmentUrl` вместе с сообщением,
  - рендер ссылки "Открыть вложение" в ленте.
- Измененные файлы:
  - `WebBlazorDeliveryCRM/Components/Shared/CompanyChatPanel.razor`
  - `WebBlazorDeliveryCRM/Services/ChatApiService.cs`
  - `APIDeliveryCRM/Controllers/FilesController.cs`
  - `APIDeliveryCRM/Services/FileService.cs`
  - `APIDeliveryCRM/Interfaces/IFileService.cs`

### 6.3 Chat AI-like stage (новый этап, выполнено частично)

- Реализованы категории быстрых ответов:
  - `greeting`, `clarification`, `sla`, `closing`.
- Реализованы персональные шаблоны (CRUD) с scope `company + user`.
- Реализован поиск по шаблонам в UI.
- Улучшен UI вложений:
  - иконка типа файла,
  - отображение имени файла,
  - preview для изображений (минимальный).
- Backend для шаблонов:
  - `APIDeliveryCRM/Model/ChatQuickReplyTemplate.cs`
  - `APIDeliveryCRM/Request/ChatQuickReplyTemplateRequests.cs`
  - `APIDeliveryCRM/Responses/ChatQuickReplyTemplateDto.cs`
  - `APIDeliveryCRM/Interfaces/IChatService.cs`
  - `APIDeliveryCRM/Services/ChatService.cs`
  - `APIDeliveryCRM/Controllers/ChatController.cs`
  - `APIDeliveryCRM/ContextDb/ContextDB.cs`
- Frontend для шаблонов и UI:
  - `WebBlazorDeliveryCRM/Services/ChatApiService.cs`
  - `WebBlazorDeliveryCRM/Components/Shared/CompanyChatPanel.razor`

Важно: для полного runtime запуска персональных шаблонов нужна миграция БД под таблицу `ChatQuickReplyTemplates`.

### 6.4 Chat AI-like stage (доработка БД + smoke)

- Создана EF миграция:
  - `APIDeliveryCRM/Migrations/20260426121521_ChatQuickReplyTemplates.cs`
  - `APIDeliveryCRM/Migrations/20260426121521_ChatQuickReplyTemplates.Designer.cs`
- Выполнен технический smoke:
  - `dotnet build APIDeliveryCRM.sln` — успешно,
  - `dotnet test APIDeliveryCRM.sln` — успешно (тестовые проекты не заполнены, падений нет).
- Текущий статус chat roadmap:
  - Категории шаблонов — готово.
  - Персональные шаблоны CRUD — готово.
  - Поиск по шаблонам — готово.
  - Улучшение вложений (иконка/имя/preview) — готово.
  - Финальный ручной smoke в UI — рекомендуется сделать перед релизом.

## 7) Миграции и data layer

За период работы создано много миграций для новых блоков:
- Orders SLA/timeline/dispatch,
- Support tickets,
- Leads 2.0,
- Notification center priority/ack,
- Service area zones,
- Vehicle docs & availability,
- Billing module + webhook idempotency,
- Communication templates,
- Scheduled reports.

Важно: в репозитории уже есть legacy warnings по старым моделям/migrations; они не блокировали сборку в ходе задач.

## 8) Стабильность сборки и известные предупреждения

- Сборка `dotnet build APIDeliveryCRM.sln` проходит.
- Часто встречаемое старое предупреждение:
  - `CS1998` в `WebBlazorDeliveryCRM/Components/Pages/Admin/Employees.razor` (не критично, не из последних изменений).
- Иногда `ReadLints` показывает Razor предупреждения, которые не воспроизводятся в фактической сборке (рассинхрон IDE diagnostics).

## 9) Release artifacts

- Добавлен файл:
  - `RELEASE_CHECKLIST.md`
- Там есть pre-deploy/smoke/billing-webhook/realtime/post-deploy мониторинг.
- Для чата добавлен отдельный финальный чеклист:
  - `CHAT_SMOKE_CHECKLIST.md`

## 10) Критичные конфиги перед продом

Обязательно заполнить в production:
- `Billing:YooKassa:WebhookSecret`
- `Billing:YooKassa:AllowedIps`

Иначе webhook будет отклоняться (production guard внедрен в контроллере).

## 11) Что осталось сделать (roadmap next)

### 11.1 Chat AI-like improvements (приоритет)

- Категории шаблонов быстрых ответов. (сделано)
- Персональные шаблоны пользователя (CRUD). (сделано)
- Поиск/фильтр шаблонов. (сделано)
- UI вложений: иконки/filename/preview для изображений. (сделано, минимально)
- (Опционально) автоподсказка шаблонов по контексту сообщения.

### 11.2 Final polish

- Финальный consistency pass по оставшимся страницам.
- Full smoke по ролям.
- Chat manual smoke по `CHAT_SMOKE_CHECKLIST.md` (последний ручной шаг перед релизом).
- Итоговый release summary.

## 12) Быстрый resume prompt после reinstall

Скопируй в новый чат:

```text
Открой файл CHAT_MEMORY_HANDOFF.md в корне репозитория и используй его как единственный источник полного контекста проекта.
Сначала проверь последние изменения по чату (CompanyChatPanel, ChatApiService, FilesController, FileService, IFileService),
затем продолжи с AI-like улучшений чата: категории шаблонов, персональные шаблоны (CRUD), поиск по шаблонам, улучшенный UI вложений.
После каждого блока запускай dotnet build APIDeliveryCRM.sln и lint-check по измененным файлам.
```

## 13) Мини-чек перед переносом/переустановкой

- Сохранить/забэкапить весь репозиторий.
- Убедиться, что в бэкапе есть:
  - `CHAT_MEMORY_HANDOFF.md`
  - `RELEASE_CHECKLIST.md`
- (Если нужно) сделать commit локальных изменений перед reinstall.

## 14) Debug/Fix log: зависания при кликах и навигации по точкам (2026-04-28)

Проблема по симптомам:
- При нажатии на часть кнопок UI мог "зависать" и требовал перезагрузку страницы.
- На странице логиста `/logistician/routes` некорректно работала навигация/переупорядочивание точек маршрута.

Анализ:
- Основная зона риска найдена в `WebBlazorDeliveryCRM/Components/Pages/Logistician/RouteOptimization.razor`.
- На странице есть последовательный геокодинг множества точек (Nominatim), длительные операции и JS interop.
- При ошибке/долгой операции пользователь визуально получал "подвисание".
- При reorder точек не было стабильного `@key`, из-за чего Blazor мог переиспользовать DOM-элементы не так, как ожидается (визуально это выглядело как "ломаная" навигация по точкам).

Что изменено (подробно):
1. **Стабилизация списка точек**
   - Добавлен стабильный ключ рендера:
     - в строке точки: `@key="_routePoints[idx].Id"`
     - в `RoutePointInput` добавлено поле `Id = Guid.NewGuid()`.
   - Эффект: корректный diff DOM при перемещении точек вверх/вниз.

2. **Защита от конфликтующих действий во время долгих операций**
   - Во время построения/оптимизации (`_isBuildingRoute = true`) блокируются:
     - перемещение точек,
     - удаление точек,
     - ручное редактирование адресов,
     - добавление точки,
     - очистка маршрута.
   - Эффект: исключены гонки состояния при параллельных кликах.

3. **Отмена/таймаут долгих операций геокодинга**
   - Добавлен `CancellationTokenSource` (`_routeCts`) с таймаутом 45 секунд.
   - Перед новым запуском операции предыдущая отменяется.
   - `BuildRouteFromPointsAsync` и `OptimizeRouteOrderAsync` переведены на работу с отменой.
   - `GeocodeAddressAsync` теперь принимает `CancellationToken`.
   - Добавлены понятные сообщения пользователю при timeout/cancel.
   - Эффект: больше нет "вечного ожидания" при проблемах сети/геокодера.

4. **Безопасный JS interop**
   - `ClearCustomRouteAsync` обернут в `try/catch` с friendly message.
   - Эффект: ошибка JS (например, карта не готова) не приводит к разрыву Blazor-circuit.

5. **Корректное освобождение ресурсов**
   - Компонент реализует `IDisposable`.
   - В `Dispose()` отменяется и освобождается `_routeCts`.
   - Эффект: нет висящих операций при уходе со страницы.

Файлы, измененные в этом фикс-пакете:
- `WebBlazorDeliveryCRM/Components/Pages/Logistician/RouteOptimization.razor`

Проверка после изменений:
- Выполнена сборка решения:
  - `dotnet build APIDeliveryCRM.sln` -> успешно.
- Проверка линтером измененного файла:
  - ошибок нет.

Ожидаемый результат для пользователя:
- Кнопки на странице маршрутов перестают "подвешивать" интерфейс.
- Навигация/переупорядочивание точек работает стабильно.
- При сетевых проблемах пользователь видит контролируемое сообщение, а не "зависание".

## 15) Реализация: сохранение позиции через Fluxor (2026-04-28)

Задача:
- Добавить сохранение позиции "игрока" через Fluxor, чтобы состояние переживало перезагрузку страницы.

Сделано:
1. Подключен Fluxor в фронтенд-проект:
   - `WebBlazorDeliveryCRM/WebBlazorDeliveryCRM.csproj`
     - добавлен пакет `Fluxor.Blazor.Web`.
   - `WebBlazorDeliveryCRM/Program.cs`
     - добавлено `builder.Services.AddFluxor(options => options.ScanAssemblies(typeof(Program).Assembly));`

2. Добавлен store-модуль `PlayerPosition`:
   - `WebBlazorDeliveryCRM/Store/PlayerPosition/PlayerPositionState.cs`
     - state: `IsLoaded`, `X`, `Y`, `UpdatedAtUtc`.
   - `WebBlazorDeliveryCRM/Store/PlayerPosition/PlayerPositionActions.cs`
     - `LoadPlayerPositionAction`, `SetPlayerPositionAction`, `PlayerPositionLoadedAction`.
   - `WebBlazorDeliveryCRM/Store/PlayerPosition/PlayerPositionReducers.cs`
     - reducers для обновления позиции и загрузки из сохраненного состояния.
   - `WebBlazorDeliveryCRM/Store/PlayerPosition/PlayerPositionEffects.cs`
     - загрузка/сохранение в `localStorage` (`player-position`) через `IJSRuntime`.
     - обработка ошибок localStorage без падения UI.

3. Подключено использование в UI:
   - `WebBlazorDeliveryCRM/Components/Pages/Counter.razor`
     - страница переведена на Fluxor-state позиции.
     - при `OnInitialized` диспатчится `LoadPlayerPositionAction`.
     - кнопки перемещения изменяют `X/Y` через `SetPlayerPositionAction`.
     - состояние сохраняется в localStorage и восстанавливается после reload.

Проверка:
- `dotnet build APIDeliveryCRM.sln` -> успешно.
- Ошибок по новым файлам нет; есть старое предупреждение `CS1998` в `Admin/Employees.razor` (legacy, не связано с Fluxor-изменениями).

## 16) Release hardening: доступ авторизованных пользователей + унификация ролей (2026-04-28)

Контекст проблемы:
- В сид-данных ролей используется `Администратор`.
- В части backend/frontend проверок использовалось `Админ`.
- Из-за расхождения роль в JWT не совпадала с `[Authorize(Roles=...)]` в отдельных местах, что могло давать `403` у корректно авторизованных пользователей.

Что сделано:
1. **Унификация role-check на backend (совместимость):**
   - Все критичные `[Authorize(Roles=...)]` расширены до совместимого набора:
     - `Администратор,Админ` (и в составе комбинированных списков).
   - Обновлены контроллеры:
     - `AuditLogsController`
     - `BillingController`
     - `ChatController`
     - `CompanySettingsController`
     - `CommunicationTemplatesController`
     - `CouriersController`
     - `LogisticsHubsController`
     - `ReportsController`
     - `ScheduledReportsController`
     - `ServiceAreaZonesController`
     - `VehiclesController`
   - В сервисах с адресным уведомлением менеджеров/админов добавлен учет `Администратор`:
     - `OrderService`
     - `ScheduledReportService`

2. **JWT role compatibility в `UserService`:**
   - В `GenerateJwtToken` добавлена совместимость:
     - если роль `Администратор`, добавляется доп. role claim `Админ`;
     - если роль `Админ`, добавляется доп. role claim `Администратор`.
   - Это предотвращает регрессии в legacy-частях UI/API.

3. **Frontend role compatibility:**
   - Обновлены переходы/проверки ролей:
     - `Login.razor` (redirect для admin)
     - `ChatRedirect.razor` (manager/admin chat routing)
     - `ManagerSidebar.razor` (`_isAdmin` для двух вариантов)
     - `Admin/Audit.razor` (`_isAdmin` для двух вариантов + текст)
   - Обновлены `@attribute [Authorize(Roles=...)]` на страницах:
     - `ManagerChat.razor`
     - `Analytics.razor`
     - `LogisticianChat.razor`
     - `ManagerCourierDetail.razor`
     - `ManagerHubs.razor`
     - `GeoAnalytics.razor`

4. **Доступ к данным только для авторизованных пользователей (release baseline):**
   - Усилена защита `UsersController`:
     - `[Authorize]` добавлен на data-endpoints (`online`, `getById`, `getAllUsers`, `GetAllManagers`, `GetAllCourier`, `UpdateUser`).
     - `Login` и регистрационные endpoints явно помечены `[AllowAnonymous]`.

Проверка:
- `dotnet build APIDeliveryCRM.sln` -> успешно.
- Линтеры по измененным файлам -> без ошибок.

## 19) Работа с замечаниями по ВКР (на основе PDF "Мубаракшин Марсель_08_04") - 2026-04-28

Выполнено "под наш проект DeliveryCRM":

1. Усилено описание и анализ предметной области
   - Дополнен файл `DIPLOMA_PART_1_ANALYTICAL.md`:
     - добавлен подраздел `1.4` с прикладной управленческой аналитикой;
     - добавлен блок про популярность услуг/типов заказов (аналог замечания про "популярность блюд");
     - добавлены рекомендации по корректировке цены на основе спроса, маржи, SLA и отмен.

2. Добавлены формы отчетов (отдельное приложение)
   - Создан файл `DIPLOMA_REPORT_FORMS.md`:
     - Ф-1 свод по заказам,
     - Ф-2 популярность услуг и рекомендации цены,
     - Ф-3 SLA по зонам,
     - Ф-4 эффективность курьеров,
     - Ф-5 клиентский CRM-отчет,
     - Ф-6 финансовый отчет по тарифам.

3. Добавлено приложение с программным кодом
   - Создан файл `DIPLOMA_APP_CODE_APPENDIX.md`:
     - перечень ключевых backend/frontend файлов;
     - рекомендации по оформлению листингов кода в приложении ВКР.

4. Подготовлена нумерация источников и шаблон ссылок
   - Создан файл `DIPLOMA_SOURCES_NUMBERED.md`:
     - нумерованный список источников;
     - пример ссылок вида `[N]` в тексте;
     - чек-лист консистентности источников и ссылок.

Проверка:
- После обновления документации выполнена сборка:
  - `dotnet build APIDeliveryCRM.sln` -> успешно (0 ошибок, 0 предупреждений).

## 20) Объединение дипломных материалов в один файл (2026-04-28)

Запрос: "пусть все будет в одном файле и пусть будет содержание".

Сделано:
- Создан единый структурированный документ:
  - `DIPLOMA_UNIFIED.md`
- В документ включены:
  - введение;
  - аналитическая часть;
  - расширенный блок по аналитике и ценообразованию;
  - формы отчетов (как приложение);
  - приложение с программным кодом;
  - нумерованный список источников;
  - правила ссылок `[N]` по тексту.
- Добавлено полноценное оглавление (содержание) с якорями для навигации.

## 21) Приведение единого файла к требованиям преподавателя (2026-04-28)

По новому списку замечаний файл `DIPLOMA_UNIFIED.md` полностью переработан:

1. Введение:
   - оставлена одна формулировка цели;
   - после цели добавлен отдельный список задач.

2. Подразделы:
   - в конце каждого подраздела добавлен явный вывод (минимум две фразы);
   - исключены окончания подразделов таблицами/списками/рисунками без выводов.

3. Структура:
   - приведена к формату пояснительной записки:
     - аналитическая часть;
     - проектирование;
     - реализация;
     - тестирование;
     - руководство пользователя;
     - заключение;
     - источники;
     - приложения А и Б.

4. База данных и SQL-логика:
   - зафиксирован правильный порядок из требований:
     - словарь данных -> `CREATE` + `INSERT` (текстом) -> фраза про приложение А -> `SELECT` (текстом).

5. Ссылки:
   - добавлены указания на обязательные ссылки на таблицы, рисунки и приложения;
   - добавлены ссылки на источники в формате `[N]`.

6. Приложения:
   - приложение А: SQL (текстом);
   - приложение Б: код; добавлена рекомендация по шрифту 10 и 2-3 колонкам.

## 22) Доработка аналитической части под формат преподавателя (2026-04-28)

По замечанию "нужны картинки и полный формат 1-й части" в `DIPLOMA_UNIFIED.md` доработана глава 1:

1. Добавлены явные места под рисунки с подписями:
   - Рисунок 1.1 - контекстная схема предметной области;
   - Рисунок 1.2 - жизненный цикл заказа;
   - Рисунок 1.3 - позиционирование среди аналогов.

2. Добавлены таблицы внутри аналитической части:
   - Таблица 1.1 - факторы популярности услуг и ценовые рекомендации;
   - Таблица 1.2 - проблемы предметной области и меры автоматизации;
   - Таблица 1.3 - сравнительный анализ аналогов.

3. Обновлены ссылки в тексте на таблицы/рисунки и выровнена нумерация таблиц главы 1.

4. Сохранено требование: каждый подраздел заканчивается выводом и не обрывается списком/таблицей.

## 23) Приведение структуры 1-й главы к "плану как в примере" (2026-04-28)

По запросу "надо именно так сделать; у аналогов короткое описание и затем скрин":

1. В `DIPLOMA_UNIFIED.md` изменена логика 1-й главы:
   - `1.2` теперь "Обзор аналогов";
   - `1.3` теперь "Анализ предметной области".

2. В подразделе `1.2` реализован формат по каждому аналогу:
   - краткое описание аналога;
   - сразу после описания подпись рисунка и место под скриншот:
     - Рисунок 1.3 (Bitrix24),
     - Рисунок 1.4 (Onfleet),
     - Рисунок 1.5 (Bringg).

3. Добавлен отдельный рисунок позиционирования:
   - Рисунок 1.6 (позиционирование DeliveryCRM среди аналогов).

4. Перенумерованы таблицы внутри главы 1:
   - Таблица 1.1 - сравнение аналогов;
   - Таблица 1.2 - факторы популярности и ценовые рекомендации;
   - Таблица 1.3 - проблемы предметной области и меры автоматизации.

Ожидаемый эффект:
- Авторизованные пользователи с ролью из сидов (`Администратор`) получают доступ к защищенным данным/разделам без ложных `403`.
- Legacy-аккаунты с ролью `Админ` тоже продолжают работать.
- Данные users-endpoints больше не отдаются анонимным запросам.

## 17) Финализация плана: security + tenant isolation (2026-04-28)

Цель этапа:
- Закрыть оставшиеся риски перед "спокойным релизом":
  - убрать доступ к данным без авторизации,
  - убрать небезопасную фильтрацию по `companyId` из query без сверки с JWT claim.

Сделано:
1. **OrdersController hardening**
   - Добавлен `[Authorize]` на контроллер.
   - Для `GET /api/Orders` добавлен `ResolveCompanyId(...)`:
     - берется `companyId` из claim,
     - query `companyId` допускается только если совпадает с claim,
     - при mismatch -> `Forbid`.

2. **LeadsController hardening**
   - Добавлен `[Authorize]` на контроллер.
   - `GetByCompany`, `Create`, `Analytics` переведены на безопасное разрешение компании через claim.
   - В `Create`:
     - `managerUserId` теперь опционален,
     - если не передан, используется текущий авторизованный пользователь.

3. **SupportTicketsController tenant-safety**
   - `GetByCompany`, `Create`, `Analytics` теперь используют `ResolveCompanyId(...)` с проверкой claim/query.
   - Исключены cross-tenant запросы по произвольному `companyId`.

4. **EmployeesController hardening**
   - Добавлен `[Authorize]`.
   - `GetByCompany` и `Create` переведены на claim-based company resolution с `Forbid` при mismatch.

5. **UsersController улучшение online-endpoint**
   - `GET /api/Users/online`:
     - `companyId` стал опциональным,
     - фильтрация всегда привязана к `companyId` из claims,
     - query mismatch -> `Forbid`.

6. **Дополнительное закрытие открытых data-endpoints**
   - Добавлен `[Authorize]` на:
     - `ClientsController`
     - `ReviewsController`
     - `ThemeController`

Проверка:
- `dotnet build APIDeliveryCRM.sln` -> успешно.
- Поведение по плану:
  - анонимный доступ к данным ограничен,
  - tenant-доступы более строгие, без доверия к внешнему `companyId`.

## 18) Дополнительное релизное усиление (2026-04-28)

Что доделано:
1. `RolesController`
   - Добавлен доступ по ролям: `[Authorize(Roles = "Менеджер,Администратор,Админ")]`.
   - Справочник ролей больше не доступен всем авторизованным без разграничения.

2. `CouriersController`
   - Добавлен `[Authorize]` на контроллер.
   - `GetAll` и `GetVehiclesByCompany` переведены на claim-based company resolution:
     - `companyId` из query сверяется с claim `companyId`,
     - при несовпадении -> `Forbid`.
   - Для `GetByUserId`, `GetProfile`, `GetActiveOrders` добавлена проверка принадлежности к компании из JWT.

3. `FilesController`
   - Для `UploadAvatar` и `UpdateAvatar` добавлена защита:
     - обычный пользователь может менять аватар только себе,
     - менеджер/админ/администратор может менять аватар другим.
   - Это закрывает риск изменения чужого профиля через подмену `userId`.

Проверка:
- `dotnet build APIDeliveryCRM.sln` -> успешно.
- Линтеры по измененным файлам -> без ошибок.

## 19) Приведение диплома к структуре эталона (2026-04-28)

Цель этапа:
- Перестроить `DIPLOMA_UNIFIED.md` по образцу `Мубаракшин Марсель_08_04` с таким же содержанием (структура глав/подразделов), но с наполнением под проект `DeliveryCRM`.

Сделано:
1. Полностью пересобран `DIPLOMA_UNIFIED.md` (удалена дублирующая и устаревшая структура, сформирована единая финальная версия).
2. Содержание приведено к требуемому порядку разделов:
   - `1 Аналитическая часть` (1.1, 1.2),
   - `2 Проектирование программного продукта` (2.1-2.5),
   - `3 Реализация проекта` (3.1-3.2),
   - `4 Тестирование программного продукта` (4.1-4.2),
   - `5 Руководство пользователя` (5.1-5.4),
   - заключение, источники, приложения А/Б/В.
3. В аналитике оставлен формат "описание аналога -> сразу место под скриншот" для каждого аналога, как в замечании преподавателя.
4. В разделе БД зафиксирована требуемая последовательность:
   - словарь данных,
   - далее текстовые `CREATE` и `INSERT`,
   - далее фраза про приложение А,
   - после этого текстовые `SELECT`.
5. Добавлены явные маркеры мест под рисунки/таблицы для последующей вставки в Word.
6. Подразделы завершены выводами и не заканчиваются списком/таблицей/рисунком.

Замечания:
- Прямое чтение `.docx` инструментом недоступно (формат бинарный), поэтому структура перенесена по доступному текстовому представлению и ранее согласованным требованиям по содержанию.

Результат:
- `DIPLOMA_UNIFIED.md` теперь соответствует требованию "содержание такое же" по каркасу и логике оформления, с тематическим заполнением под `DeliveryCRM`.

## 20) Проверка замечаний преподавателя и расширение глав 1.2-5.4 (2026-04-28)

Цель этапа:
- Убрать замечания по пояснительной записке: расширить текст, усилить обзор аналогов, проверить порядок SQL-раздела и наличие обязательных ссылок на рисунки/таблицы/приложения.

Сделано:
1. Существенно расширен подраздел `1.2 Обзор аналогов`:
   - добавлен вводный абзац о методике анализа;
   - для каждого аналога дано отдельное развернутое описание;
   - добавлены блоки "Плюсы/Минусы";
   - сохранен формат "описание -> место для рисунка";
   - добавлено заключение по разделу в виде обычного абзаца.
2. Усилен раздел `2.1 Определение требований к системе`:
   - добавлены архитектурные, безопасностные и интерфейсные требования;
   - расширены функциональные/нефункциональные требования;
   - исправлена опечатка в начале подраздела.
3. Усилены подразделы `2.2-2.5`:
   - добавлены пояснения по API-контруру и согласованности бизнес-логики;
   - добавлены ссылки вида `(см. рисунок X.X)` и `(см. таблицу X.X)`;
   - расширены итоговые абзацы подразделов.
4. Приведен в полный порядок подраздел `2.3 Проектирование БД`:
   - явно указано требование к словарю данных (шрифт 12, интервал 1);
   - добавлены места для таблиц словаря `2.2-2.10`;
   - сохранена последовательность: словарь -> `CREATE/INSERT` -> фраза про приложение А -> `SELECT`;
   - добавлены ссылки на приложения А и Б в тексте.
5. Расширены главы `3`, `4`, `5`:
   - добавлены содержательные абзацы для каждого подраздела;
   - добавлены ссылки на рисунки/таблицы внутри текста;
   - сохранено правило: подраздел не заканчивается рисунком/таблицей/списком.
6. Обновлены формулировки приложений:
   - приложение А: данные таблиц и полный SQL;
   - приложение Б: уточнено оформление шрифтом 10 в 2-3 колонки.

Проверка:
- По `DIPLOMA_UNIFIED.md` не найдены запрещенные слова `я`, `мы`, `было`.
- По тексту присутствуют ссылки на рисунки/таблицы/приложения.
- Критичные замечания по структуре и последовательности SQL-раздела закрыты.

## 21) Замена заглушек на рабочий функционал в WebBlazorDeliveryCRM (2026-04-30)

Цель этапа:
- Найти страницы-заглушки во фронтенде и заменить их на рабочие экраны с реальными данными из API.

Сделано:
1. `Customer/CreateOrder.razor`
   - Вместо заглушки реализована полноценная форма создания заказа.
   - Добавлена отправка данных в API через `OrdersApiService.CreateMineAsync`.
2. Backend: `OrdersController`
   - Добавлен endpoint `POST /api/Orders/create-mine` для создания заказа текущим клиентом.
   - Добавлены создание адресов забора/доставки и формирование `CreateOrderRequest` на основе профиля клиента.
3. `Customer/Profile.razor`
   - Подключена загрузка профиля текущего клиента.
   - Добавлены агрегаты по заказам (количество, дата последнего заказа).
4. `Customer/OrderTracking.razor`
   - Заглушка заменена на рабочее отслеживание:
     - список заказов клиента;
     - загрузка таймлайна по выбранному заказу.
5. `Customer/CustomerShifts.razor`
   - Заглушка заменена на фактическую метрику доступности курьеров компании (всего/онлайн/процент).
6. `Logistician/ShiftPlanning.razor`
   - Черновая заглушка заменена на рабочий список курьеров по компании с текущим статусом и последней активностью.
   - KPI-блок переведен на реальные значения.

Проверка:
- IDE lints по измененным страницам: без ошибок.
- Сборка локально не завершена из-за блокировки бинарников запущенными процессами (`WebBlazorDeliveryCRM.exe`, `APIDeliveryCRM.exe`), кодовые изменения при этом применены.


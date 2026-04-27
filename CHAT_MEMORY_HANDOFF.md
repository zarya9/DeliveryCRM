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


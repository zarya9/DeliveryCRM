# Пакеты WebBlazorDeliveryCRM (.NET 8.0)

## Установлены (`WebBlazorDeliveryCRM.csproj`)

```xml
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="8.0.11" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.11" />
<PackageReference Include="MudBlazor" Version="9.2.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
<PackageReference Include="Blazored.Toast" Version="4.2.1" />
```

**Почему MudBlazor, а не Radzen:** больше примеров под Blazor Server, единый Material UI для CRM, актуальная поддержка **.NET 8** (линия 9.x).

- **MudBlazor** — тема, `MudTable`, `MudButton`, диалоги; в `App.razor`: шрифт Roboto, `MudBlazor.min.css`, `MudBlazor.min.js`. В **MudBlazor 9+** провайдеры **без вложенности** (у `MudThemeProvider` нет `ChildContent`): `<MudThemeProvider />`, `<MudPopoverProvider />`, `<MudDialogProvider />`, затем приложение (**без** `MudSnackbarProvider` — toasts через **Blazored.Toast**).

---

## Аутентификация (JWT только в HttpOnly-cookie)

| Роль | Реализация |
|------|------------|
| Хранение токена | Cookie **`auth_token`**: `HttpOnly`, `SameSite=Lax`, в проде **`Secure`**. **Не** localStorage — XSS не читает JWT из JS. |
| Запись cookie | После успешного `POST /api/Users/Login` к API браузер вызывает `POST /api/auth/session` с телом `{ "token": "..." }` (`wwwroot/js/authCookie.js`) — ответ Blazor выставляет cookie. |
| Состояние входа | `CustomAuthenticationStateProvider` читает cookie из `HttpContext.Request` (на сервере). |
| HttpClient к API | `AuthorizationMessageHandler` подставляет `Bearer` из той же cookie. |
| SignalR | `ChatHubClientService` — токен из `HttpContext.Request.Cookies`. |
| Выход | `POST /api/auth/logout` + `MarkUserAsLoggedOutAsync()` (очистка через JS). |

**Ограничение:** при входе JWT кратко передаётся в JS для `fetch` тела запроса — это единственный момент в браузере; в persistent storage он не кладётся.

---

## Уведомления

| Роль | Пакет / код |
|------|-------------|
| Toast в UI | **Blazored.Toast** — `IToastService`, `<ToastView />` в `MainLayout` / `LoginLayout` |
| Сервис приложения | `AppNotificationService` — обёртка над `IToastService` + **JS** `wwwroot/js/notifications.js` |
| Push, когда вкладка скрыта | Тот же JS (браузерный `Notification`) |
| **Вкладка «Уведомления»** (`/manager/notifications`) | API `GET /api/Notifications/me`, `POST .../read`; в БД таблица `Notifications`; **SignalR** `NotificationReceived` на группу `User_{id}` при новом событии (сейчас — новые сообщения в чатах из `ChatService`). |
| **Онлайн сотрудников** (страница «Сотрудники») | Без опроса: **SignalR** `UserPresenceChanged` по группе компании `Company_{id}` из `ChatHub`. |

**Важно:** не дублировать те же сообщения через **MudSnackbar** без рефакторинга `AppNotificationService`.

В `_Imports.razor`:

```razor
@using Blazored.Toast
@using Blazored.Toast.Services
```

---

## Карта и маршруты (Leaflet + OSRM)

| Что | Роль |
|-----|------|
| **Leaflet** | Карта в браузере (`App.razor`, `wwwroot/js/leafletMap.js`). |
| **Тайлы** | OpenStreetMap / CARTO — только **подложка** (картинка карты). |
| **OSRM** | Движок маршрутов: `route/v1/driving/...`. |
| **Leaflet Routing Machine** | Панель пошаговых инструкций (`L.Routing.control` + `Formatter`); **синяя линия маршрута** рисуется отдельным запросом к OSRM (`overview=full`, `geometries=geojson` в `leafletMap.js` → `drawRouteOsrm`), чтобы линия шла **по дорогам**, а не прямыми как у «пустой» линии LRM при сбое/лимите. У роутера задано `requestParameters: { overview: "full", geometries: "geojson" }`. Язык: **`Map:RouteLanguage`** в `appsettings` (например `ru`). На карте менеджера маршрут по клику **отключён** (`enableRouting: false`). |

Настройка URL роутера: **`Osrm:RouterBaseUrl`** в `appsettings.json` (по умолчанию публичный демо `https://router.project-osrm.org`; для продакшена лучше свой инстанс).

**Свой OSRM (Docker), пример:**

```bash
docker run -t -i -p 5000:5000 -v "${PWD}:/data" osrm/osrm-backend osrm-routed --algorithm mld /data/your-region.osrm
```

Укажите в `appsettings.Development.json`: `"RouterBaseUrl": "http://localhost:5000"`. Если браузер блокирует запрос из‑за CORS, проксируйте OSRM через ваш API или включите CORS на контейнере.

### База данных API (PostgreSQL)

Если при сохранении транспорта ошибка **`столбец "Brand_name" в таблице "Vehicles" не существует`** — в БД не применены миграции EF. Из каталога **`APIDeliveryCRM`** (где `APIDeliveryCRM.csproj`):

```bash
dotnet ef database update
```

Нужна миграция **`VehicleManualBrandModel`** (колонки `Brand_name`, `Model_name`, `Model_id` nullable). Без этого `INSERT` из `CourierService.CreateVehicleAsync` не пройдёт.

---

## Дополнительно (по желанию)

| Что | Зачем |
|-----|--------|
| **Radzen.Blazor** | Гриды/чарты рядом с Mud — осторожно со стилями |
| **Blazorise** + Bootstrap | Усилить Bootstrap без Material |
| Иконки (CDN) | Bootstrap Icons — без NuGet |

**Не дублировать:** второй полноценный toast-пакет для тех же событий.

---

## Кратко

1. **Blazored.Toast** — уведомления.  
2. **MudBlazor** — UI; toasts не трогаем.  
3. **JWT** — только HttpOnly-cookie + серверные обработчики.

Версия .NET: **8.0** (не менять).

## Проверка NuGet

```bash
dotnet restore APIDeliveryCRM.sln
```

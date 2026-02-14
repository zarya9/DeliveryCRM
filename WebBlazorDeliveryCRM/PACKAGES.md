# Пакеты WebBlazorDeliveryCRM (.NET 8.0)

## Уже установлены

```xml
<PackageReference Include="Blazored.LocalStorage" Version="4.4.0" />
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="8.0.11" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.11" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
```

- **Blazored.LocalStorage** — хранение JWT в браузере (localStorage), токен сохраняется между сессиями.
- **Microsoft.AspNetCore.Components.Authorization** — `AuthenticationStateProvider`, `AuthorizeRouteView`, `CascadingAuthenticationState`.
- **Microsoft.AspNetCore.SignalR.Client** — клиент SignalR для подключения к ChatHub API.
- **System.IdentityModel.Tokens.Jwt** — разбор JWT для получения claims (имя, роль, email).

## При необходимости можно добавить

- **MudBlazor** или **Radzen.Blazor** — готовые UI-компоненты (таблицы, формы, диалоги).
- **Fluxor** — state management (если понадобится глобальное состояние).
- **Blazored.Toast** — уведомления (toast) после логина/ошибок.

Версия .NET в проекте: **8.0** (не менять).

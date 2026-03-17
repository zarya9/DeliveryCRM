# Развёртывание: PostgreSQL + API на сервер

## 1. PostgreSQL на сервере

### Вариант A: Свой сервер (VPS) с установленным PostgreSQL

**Установка PostgreSQL (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

**Создание БД и пользователя:**
```bash
sudo -u postgres psql
```

В консоли PostgreSQL:
```sql
CREATE USER deliverycrm_user WITH PASSWORD 'ваш_надёжный_пароль';
CREATE DATABASE deliverycrm OWNER deliverycrm_user;
GRANT ALL PRIVILEGES ON DATABASE deliverycrm TO deliverycrm_user;
\c deliverycrm
GRANT ALL ON SCHEMA public TO deliverycrm_user;
\q
```

**Строка подключения** (подставьте хост, порт, пароль):
```
Host=ваш_сервер;Port=5432;Database=deliverycrm;Username=deliverycrm_user;Password=ваш_пароль
```

Если API и PostgreSQL на одном сервере, можно использовать `Host=localhost`.

---

### Вариант B: Облачный PostgreSQL (без своего сервера БД)

- **Neon** (https://neon.tech) — бесплатный тир, даёт строку подключения.
- **Supabase** (https://supabase.com) — PostgreSQL + хостинг.
- **Azure Database for PostgreSQL** / **AWS RDS** / **DigitalOcean Managed DB** — платные.

После создания инстанса скопируйте **connection string** — он будет вида:
`Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;`

---

## 2. Применение миграций к БД (схема на PostgreSQL)

На своей машине (где есть проект и .NET 8):

1. Задать строку подключения к **целевой** БД (на сервере или в облаке).

2. В корне решения выполнить (из папки с .csproj API):
```bash
cd APIDeliveryCRM
dotnet ef database update
```

Если `dotnet ef` не найден, установить глобально:
```bash
dotnet tool install --global dotnet-ef
```

Строку подключения можно передать так:

**Через переменную окружения:**
```bash
export ConnectionStrings__DefaultConnection="Host=ваш_хост;Port=5432;Database=deliverycrm;Username=...;Password=...;"
dotnet ef database update --project APIDeliveryCRM
```

**Или временно в `appsettings.Development.json`** в APIDeliveryCRM — указать нужную строку, выполнить `dotnet ef database update`, затем не коммитить пароли в репозиторий.

После выполнения миграций таблицы появятся в выбранной БД на PostgreSQL.

---

## 3. Публикация API (сборка для сервера)

На своей машине:

```bash
cd D:\Zarya\APIDeliveryCRM\APIDeliveryCRM
dotnet publish -c Release -o ./publish
```

В папке `publish` будет готовый к запуску набор файлов (исполняемый файл или `APIDeliveryCRM.dll` + зависимости).

---

## 4. Размещение API на сервере

### Вариант A: Linux (systemd)

1. Скопировать содержимое `publish` на сервер, например в `/var/www/apideliverycrm`:
   ```bash
   scp -r ./publish/* user@ваш_сервер:/var/www/apideliverycrm/
   ```

2. На сервере задать конфиг через переменные (рекомендуется) или через `appsettings.json` в папке приложения.

   **Файл окружения** `/var/www/apideliverycrm/appsettings.Production.json` (или переменные):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=deliverycrm;Username=deliverycrm_user;Password=ВАШ_ПАРОЛЬ"
     },
     "Jwt": {
       "Key": "ваш-секретный-ключ-не-короче-32-символов",
       "Issuer": "DeliveryCRM",
       "Audience": "DeliveryCRM"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

   Либо через переменные окружения перед запуском:
   ```bash
   export ConnectionStrings__DefaultConnection="Host=...;Database=...;..."
   export Jwt__Key="..."
   export Jwt__Issuer="DeliveryCRM"
   export Jwt__Audience="DeliveryCRM"
   ```

3. Создать systemd-сервис `/etc/systemd/system/apideliverycrm.service`:
   ```ini
   [Unit]
   Description=APIDeliveryCRM
   After=network.target postgresql.service

   [Service]
   WorkingDirectory=/var/www/apideliverycrm
   ExecStart=/usr/bin/dotnet /var/www/apideliverycrm/APIDeliveryCRM.dll
   Restart=always
   RestartSec=5
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

   [Install]
   WantedBy=multi-user.target
   ```

   Если .NET установлен в другое место, замените `/usr/bin/dotnet` на результат `which dotnet`.

4. Запуск и автозапуск:
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable apideliverycrm
   sudo systemctl start apideliverycrm
   sudo systemctl status apideliverycrm
   ```

5. Проксирование через Nginx (по желанию) на порт 80/443 и на порт приложения (например 5000).

---

### Вариант B: Развёртывание в IIS (Windows Server)

#### Шаг 1. Установка на сервер

1. **IIS** с модулями:
   - Включить «Службы IIS» (Панель управления → Программы → Включение или отключение компонентов Windows).
   - Установить **IIS** и подкомпоненты: веб-сервер, общие функции HTTP, разборщик размещения ASP.NET Core.

2. **Hosting Bundle для .NET 8** (нужен для работы ASP.NET Core под IIS):
   - Скачать: https://dotnet.microsoft.com/download/dotnet/8.0 — раздел **Hosting** (Windows).
   - Установить, перезапустить IIS (или выполнить `iisreset` от администратора).

#### Шаг 2. Публикация API

На своей машине (в папке решения):

```powershell
cd D:\Zarya\APIDeliveryCRM\APIDeliveryCRM
dotnet publish -c Release -o D:\Zarya\APIDeliveryCRM\publish-iis
```

Либо публикация сразу в папку на сервере (если есть общий диск или развёртывание по сети):

```powershell
dotnet publish -c Release -o \\сервер\c$\inetpub\apideliverycrm
```

В папке публикации должны быть: `APIDeliveryCRM.dll`, `web.config`, все зависимости, `appsettings.json`. Файл `web.config` в проекте API уже настроен для IIS (модуль AspNetCore, inprocess). При необходимости логи приложения появятся в подпапке `logs` в каталоге сайта.

#### Шаг 3. Конфиг приложения на сервере

В папке приложения (например `C:\inetpub\apideliverycrm`) создать или отредактировать **appsettings.Production.json**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=deliverycrm;Username=deliverycrm_user;Password=ВАШ_ПАРОЛЬ"
  },
  "Jwt": {
    "Key": "ваш-секретный-ключ-минимум-32-символа-длинный",
    "Issuer": "DeliveryCRM",
    "Audience": "DeliveryCRM"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Либо задать те же параметры через **переменные окружения** в пуле приложений (см. ниже) или через «Переменные окружения» в настройках пула (IIS 10+).

#### Шаг 4. Настройка IIS

1. **Диспетчер IIS** (Win + R → `inetmgr`).

2. **Пул приложений:**
   - Правый клик по «Пулы приложений» → «Добавить пул приложений».
   - Имя: например `APIDeliveryCRM`.
   - Версия .NET CLR: **«Без управляемого кода»** (обязательно для ASP.NET Core).
   - Нажать OK.
   - По желанию: правый клик по пулу → «Дополнительные параметры» → «Удостоверение» — указать учётную запись с доступом к папке приложения и к БД (если нужно).

3. **Сайт или приложение:**
   - **Вариант «Отдельный сайт»:** правый клик по «Сайты» → «Добавить веб-сайт»:
     - Имя: `APIDeliveryCRM`.
     - Пул приложений: выбранный выше пул.
     - Путь: папка с опубликованным приложением (например `C:\inetpub\apideliverycrm`).
     - Привязка: порт 80 (или 443 для HTTPS), при необходимости — имя узла (домен).
   - **Вариант «Приложение под существующим сайтом»:** правый клик по нужному сайту → «Добавить приложение»:
     - Псевдоним: например `api`.
     - Пул: тот же пул.
     - Путь к папке: папка с publish.

4. **Проверить права:** у учётной записи пула приложений (по умолчанию `IIS AppPool\APIDeliveryCRM`) должен быть доступ на чтение к папке приложения.

#### Шаг 5. Запуск и проверка

- В IIS нажать «Пуск» для сайта/приложения.
- В браузере открыть, например: `http://localhost/api` (если добавили приложение с псевдонимом `api`) или `http://ваш-сервер` (если отдельный сайт на 80 порту).
- Swagger (если включён в Production): `http://ваш-сервер/api/swagger` или `http://ваш-сервер/swagger`.

Если 502/503 — проверить: установлен ли Hosting Bundle, пул в режиме «Без управляемого кода», путь к папке и логи в папке приложения (или в Журнале событий Windows).

#### Шаг 6. SignalR и WebSockets (для чата)

В IIS для WebSockets должно быть включено:

- «Службы IIS» → «Служба веб-сервера» → «Разработка приложений» → **«Протокол WebSocket»** — включить.
- Перезапустить IIS.

---

### Вариант C: Windows — запуск как консоль/служба (без IIS)

Запуск напрямую через `dotnet APIDeliveryCRM.dll` с переменными окружения (ConnectionStrings, Jwt) или с `appsettings.Production.json` в папке приложения. Для работы как службы можно использовать **NSSM** или **sc.exe** с указанием пути к `dotnet` и `APIDeliveryCRM.dll`.

---

## 5. Проверка

- Открыть в браузере: `http://ваш_сервер:5000/swagger` (если Swagger включён в Production).
- Проверить логин через Blazor, указав в нём `ApiBaseUrl`: `http://ваш_сервер:5000` (или ваш домен/порт).

---

## 6. Краткий чек-лист

| Шаг | Действие |
|-----|----------|
| 1 | Развернуть PostgreSQL (свой сервер или облако). |
| 2 | Создать БД и пользователя, получить connection string. |
| 3 | Выполнить `dotnet ef database update` с этой строкой подключения. |
| 4 | Задать на сервере Production: ConnectionStrings, Jwt (Key, Issuer, Audience). |
| 5 | Опубликовать API: `dotnet publish -c Release -o ./publish`. |
| 6 | Скопировать `publish` на сервер и запустить (systemd / IIS / служба). |
| 7 | В Blazor в `appsettings.json` указать `ApiBaseUrl` на адрес вашего API. |

**Важно:** пароли и JWT Key не храните в репозитории; используйте переменные окружения или секреты на сервере.

# 🚀 Быстрый старт: Azure Blob Storage

## ✅ Что уже сделано

1. ✅ Установлен NuGet пакет `Azure.Storage.Blobs`
2. ✅ Создан интерфейс `IAzureBlobService`
3. ✅ Создан сервис `AzureBlobService` для работы с Azure Blob Storage
4. ✅ Обновлен `FileService` - теперь поддерживает Azure Blob Storage
5. ✅ Обновлен `Program.cs` - зарегистрирован новый сервис
6. ✅ Обновлен `appsettings.json` - добавлены настройки

## 📝 Что нужно сделать сейчас

### Шаг 1: Создать Azure Storage Account

1. Откройте [Azure Portal](https://portal.azure.com)
2. Нажмите **"Создать ресурс"** → найдите **"Storage account"**
3. Заполните:
   - **Имя**: `deliverycrmstorage` (или другое уникальное)
   - **Регион**: выберите ближайший
   - **Производительность**: Standard
   - **Избыточность**: LRS
4. Нажмите **"Создать"**

### Шаг 2: Получить Connection String

1. Откройте созданный Storage Account
2. В меню слева: **"Ключи доступа"** (Access keys)
3. Скопируйте **"Строка подключения"** из **key1**

### Шаг 3: Настроить appsettings.json

Откройте `APIDeliveryCRM/appsettings.json` и вставьте ваш Connection String:

```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
    "ContainerName": "deliverycrm"
  }
}
```

### Шаг 4: Запустить проект

```bash
cd APIDeliveryCRM
dotnet run
```

### Шаг 5: Протестировать

1. Откройте Swagger UI: `https://localhost:5001/swagger` (или ваш порт)
2. Авторизуйтесь через `/api/Users/Login`
3. Загрузите аватар через `/api/Files/avatar?userId=1`
4. Проверьте в Azure Portal → Storage Account → Контейнеры → `deliverycrm`

## 🔄 Как это работает

### Гибридный режим

Система автоматически определяет, использовать Azure или локальное хранилище:

- **Если `AzureStorage:ConnectionString` заполнен** → используется Azure Blob Storage
- **Если пустой** → используется локальная папка `wwwroot`

### Что хранится в Azure

- **Аватары**: `avatars/user_{userId}_{timestamp}.jpg`
- **Отчеты**: `reports/report_{userId}_{type}_{timestamp}.pdf`

### URL файлов

После загрузки в Azure, в базе данных сохраняется полный URL:
```
https://{account}.blob.core.windows.net/deliverycrm/avatars/user_1_20250108120000.jpg
```

## 🐛 Решение проблем

### Ошибка: "Azure Blob Storage не настроен"

**Решение**: Проверьте, что `ConnectionString` правильно заполнен в `appsettings.json`

### Ошибка: "Контейнер не найден"

**Решение**: Контейнер создается автоматически при первом запуске. Если ошибка, создайте вручную:
1. Azure Portal → Storage Account → Контейнеры
2. Нажмите **"+ Контейнер"**
3. Имя: `deliverycrm`
4. Уровень доступа: **"Blob"**

### Файлы не загружаются

**Решение**: 
1. Проверьте логи в консоли
2. Убедитесь, что Connection String правильный
3. Проверьте права доступа к Storage Account

## 📚 Дополнительно

- Подробная инструкция: `AZURE_BLOB_STORAGE_STEPS.md`
- Примеры кода: `KAFKA_AZURE_EXAMPLES.md`
- Общая информация: `KAFKA_AZURE_INTEGRATION.md`

## ✨ Преимущества

- ✅ Неограниченное хранилище
- ✅ Высокая доступность
- ✅ CDN для быстрой загрузки
- ✅ Автоматическое масштабирование
- ✅ Дешевле, чем серверное хранилище

---

**Готово!** Теперь ваши файлы хранятся в Azure Blob Storage! 🎉


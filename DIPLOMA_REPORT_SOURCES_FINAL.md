# Список источников для отчёта (замена проблемных пунктов)

Ниже — **итоговый список из 13 позиций** для раздела «Список использованных источников» в Word.  
**Изменены только п. 2, 12 и 13** (остальные как у вас в отчёте).

---

## СПИСОК ИСПОЛЬЗОВАННЫХ ИСТОЧНИКОВ (копировать в отчёт)

1. Microsoft Learn. ASP.NET Core Blazor documentation [Электронный ресурс]. URL: https://learn.microsoft.com/aspnet/core/blazor/

2. Salesforce, Inc. What is CRM? Customer relationship management defined [Электронный ресурс]. URL: https://www.salesforce.com/crm/what-is-crm/

3. Microsoft Learn. ASP.NET Core authentication and authorization [Электронный ресурс]. URL: https://learn.microsoft.com/aspnet/core/security/

4. Microsoft Learn. ASP.NET Core Web API documentation [Электронный ресурс]. URL: https://learn.microsoft.com/aspnet/core/web-api/

5. Microsoft Learn. Entity Framework Core documentation [Электронный ресурс]. URL: https://learn.microsoft.com/ef/core/

6. Microsoft Learn. EF Core relationships [Электронный ресурс]. URL: https://learn.microsoft.com/ef/core/modeling/relationships

7. PostgreSQL Global Development Group. PostgreSQL Documentation [Электронный ресурс]. URL: https://www.postgresql.org/docs/

8. Npgsql Team. Npgsql Documentation [Электронный ресурс]. URL: https://www.npgsql.org/doc/

9. Bitrix24. Официальный сайт [Электронный ресурс]. URL: https://www.bitrix24.ru/

10. Onfleet. Official Website [Электронный ресурс]. URL: https://onfleet.com/

11. Google Sheets. Официальный сервис [Электронный ресурс]. URL: https://www.google.com/sheets/about/

12. IBM. What is an entity relationship diagram? [Электронный ресурс]. URL: https://www.ibm.com/think/topics/entity-relationship-diagram

13. IBM. What is a use case diagram? [Электронный ресурс]. URL: https://www.ibm.com/think/topics/use-case-diagram

---

## Что заменено по сравнению с прежней версией

| № | Было | Стало | Зачем |
|---|------|--------|--------|
| 2 | SignalR (не подходило к абзацу про CRM в п. 1.1) | Salesforce, «What is CRM» | Соответствует смыслу ссылки **[2]** в описании предметной области |
| 12 | Lucidchart | IBM Think (ERD) | Та же тема (ER-диаграмма), проще найти по запросу «IBM entity relationship diagram» |
| 13 | Visual Paradigm | IBM Think (Use Case) | Та же тема (Use Case), стабильный URL |

Нумерация **1–13 не менялась** — правки в тексте отчёта по номерам ссылок не требуются.

---

## Фразы для вставки в текст (чтобы п. 5–8, 12–13 реально использовались)

**В начале раздела 2.2 (после первого абзаца про ER-диаграмму):**  
«При построении логической модели данных учитывались рекомендации по проектированию сущностей, связей и ограничений целостности [12]; в реализации персистентного слоя предполагается использование объектно-реляционного отображения средствами EF Core и СУБД PostgreSQL [5], [7], а также драйвера Npgsql для доступа к PostgreSQL из .NET-приложений [8]. Для настройки связей между сущностями на уровне ORM использованы материалы по моделированию отношений в EF Core [6].»

**В начале раздела 2.3 (после определения Use Case):**  
«Терминология и базовые отношения между акторами и прецедентами приведены в соответствии с общепринятым описанием диаграмм вариантов использования [13].»

При необходимости сократите вторую фразу до одного предложения — главное, чтобы в тексте появились **[5]**, **[6]**, **[7]**, **[8]**, **[12]**, **[13]**.

---

## Примечание про SignalR

Документацию SignalR из списка убрали, потому что в вашем фрагменте отчёта **на неё не было ссылок [2] по смыслу**. Если в других главах вы описываете чат/уведомления в реальном времени, добавьте **отдельным номером 14** (не ломая текущие 1–13):

14. Microsoft Learn. ASP.NET Core SignalR documentation [Электронный ресурс]. URL: https://learn.microsoft.com/aspnet/core/signalr/

И в соответствующем абзаце поставьте **[14]**.

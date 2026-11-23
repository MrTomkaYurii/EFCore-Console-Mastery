# Шпаргалка лектора: Глава 8. Додаткові теми

> **Фокус глави:** продакшен-патерни EF Core — оптимістичний паралелізм, інфраструктура логування (`ILoggerFactory`), оптимізація через скомпільовані запити, робота з SQL Views та аудит / історія змін.

## План проведення та запуски

| ID | Тема та конспект | Про що говорити (ключові тези) | Команда запуску та CLI | На що звернути увагу у виводі |
|---|---|---|---|---|
| **8.1** | [Паралелізм та оптимістичне блокування](8_01_Concurrency.md) · [код](../../src/08_AdvancedTopics/Lesson_8_01_Concurrency.cs) | • Проблема **Lost Update** (два користувачі одночасно редагують один рядок).<br>• `[ConcurrencyCheck]` додає старе значення поля у `WHERE` запиту `UPDATE`.<br>• **Нативний `[Timestamp]` (`byte[] RowVersion`) у MS SQL Server:** сервер сам оновлює 8-байтний `rowversion` при кожному `UPDATE`, і застарілий запит кидає `DbUpdateConcurrencyException`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 8.1` | • У Секції 2 видно, як змінюється hex-значення `RowVersion` після `SaveChanges()` і як другий контекст отримує `DbUpdateConcurrencyException`. |
| **8.2** | [Провайдери логування](8_02_LoggingProviders.md) · [код](../../src/08_AdvancedTopics/Lesson_8_02_LoggingProviders.cs) | • Відмінність від простого `LogTo()` (урок `1.7`): інтеграція з екосистемою `Microsoft.Extensions.Logging` (`ILoggerFactory` / `ILoggerProvider`).<br>• **Важливо для продакшену:** `ILoggerFactory` треба створювати як **singleton (static)**, а не в кожному `OnConfiguring` (інакше витік пам'яті).<br>• Власний `ILoggerProvider`, фільтрація `DbLoggerCategory.Database.Command` та вбудований Console Logger. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 8.2`<br><br>*(Пакет уже в Core)*:<br>`dotnet add package Microsoft.Extensions.Logging.Console` | • Формат виводу кастомного провайдера проти вбудованого `AddConsole()`. |
| **8.3** | [Скомпільовані запити](8_03_CompiledQueries.md) · [код](../../src/08_AdvancedTopics/Lesson_8_03_CompiledQueries.cs) | • На що витрачається час у звичайному LINQ-запиті: обчислення хешу дерева виразів та пошук у кеші компіляції.<br>• `EF.CompileQuery(...)` та `EF.CompileAsyncQuery(...)` — зберігаємо делегат у `static readonly` полі.<br>• Коли виправдано: «гарячі» запити з високим RPS; для разових запитів приріст мінімальний. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 8.3` | • Секція 3: живий бенчмарк (200 ітерацій) — порівняння часу звичайного LINQ проти `EF.CompileQuery`. |
| **8.4** | [Проєкція на представлення (Views)](8_04_DatabaseViews.md) · [код](../../src/08_AdvancedTopics/Lesson_8_04_DatabaseViews.cs) | • Навіщо `CREATE VIEW`: інкапсуляція складних `JOIN`/агрегацій на стороні БД.<br>• Мапінг в EF Core: `HasNoKey().ToView("View_ProductsByCompany", "l8_04")` (Keyless Entity Type).<br>• Keyless-сутності **ніколи не відстежуються** (`ChangeTracker` порожній) і працюють лише на читання, але поверх них повноцінно працює LINQ (`Where`, `OrderBy`). | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 8.4` | • Секція 2: `ChangeTracker.Entries().Count() == 0` навіть без `AsNoTracking()`. |
| **8.5** | [Зберігання історії змін](8_05_ChangeHistory.md) · [код](../../src/08_AdvancedTopics/Lesson_8_05_ChangeHistory.cs) | • **Нативні Temporal Tables у MS SQL Server:** `ToTable("Users", "l8_05", t => t.IsTemporal())` вмикає `SYSTEM_VERSIONING = ON` і автоматично створює таблицю історії.<br>• LINQ-методи подорожі в часі: `TemporalAll()` (уся історія) та `TemporalAsOf(utcTime)` (зріз на конкретну мілісекунду). | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 8.5` | • Секція 2 виводить усі версії рядка (`Tom -> Bob -> Sam -> DELETE`) із `PeriodStart`/`PeriodEnd`, а Секція 3 — точний зріз `TemporalAsOf`. |

---

## Наскрізна перевірка всього курсу (фінал навчання)

```powershell
# Прогнати всі 58 уроків поспіль і переконатися, що кожен завершується зі статусом OK
dotnet run --project src/EFCoreMastery.ConsoleApp -- --all
```

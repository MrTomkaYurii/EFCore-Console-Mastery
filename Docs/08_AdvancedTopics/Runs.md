# Шпаргалка лектора: Глава 8. Додаткові теми

> **Фокус глави:** продакшен-патерни EF Core — оптимістичний паралелізм, інфраструктура логування (`ILoggerFactory`), оптимізація через скомпільовані запити, робота з SQL Views та аудит / історія змін.

## План проведення та запуски

| ID | Тема та конспект | Про що говорити (ключові тези) | Команда запуску та CLI | На що звернути увагу у виводі |
|---|---|---|---|---|
| **2.11** | [Паралелізм та оптимістичне блокування](2_11_Concurrency.md) · [код](../../src/08_AdvancedTopics/Lesson_2_11_Concurrency.cs) | • Проблема **Lost Update** (два користувачі одночасно редагують один рядок).<br>• Песимістичне vs оптимістичне блокування (EF Core використовує оптимістичне).<br>• `[ConcurrencyCheck]` / `IsConcurrencyToken()` додає старе значення поля у `WHERE` запиту `UPDATE`.<br>• Якщо рядок уже змінили → `DbUpdateConcurrencyException`.<br>• **Нюанс SQLite:** немає автооновлення `rowversion`/`[Timestamp]`, як у SQL Server, тому токен версії (наприклад, `Guid` або лічильник) оновлюємо явно або в `SaveChanges`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 2.11` | • Успішне перехоплення `DbUpdateConcurrencyException` при конфлікті двох контекстів.<br>• Пояснити стратегії вирішення: *Client Wins*, *Database Wins* або ручний merge. |
| **2.12** | [Провайдери логування](2_12_LoggingProviders.md) · [код](../../src/08_AdvancedTopics/Lesson_2_12_LoggingProviders.cs) | • Відмінність від простого `LogTo()` (урок `1.6`): інтеграція з екосистемою `Microsoft.Extensions.Logging` (`ILoggerFactory` / `ILoggerProvider`).<br>• **Важливо для продакшену:** `ILoggerFactory` треба створювати як **singleton (static)**, а не в кожному `OnConfiguring` (інакше витік пам'яті).<br>• Власний `ILoggerProvider`, фільтрація `DbLoggerCategory.Database.Command` та вбудований Console Logger. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 2.12`<br><br>*(Пакет уже в Core)*:<br>`dotnet add package Microsoft.Extensions.Logging.Console` | • Формат виводу кастомного провайдера проти вбудованого `AddConsole()`. |
| **8.1** | [Скомпільовані запити](8_1_CompiledQueries.md) · [код](../../src/08_AdvancedTopics/Lesson_8_1_CompiledQueries.cs) | • На що витрачається час у звичайному LINQ-запиті: обчислення хешу дерева виразів та пошук у кеші компіляції.<br>• `EF.CompileQuery(...)` та `EF.CompileAsyncQuery(...)` — зберігаємо делегат у `static readonly` полі.<br>• Коли виправдано: «гарячі» запити з високим RPS; для разових запитів приріст мінімальний. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 8.1` | • Секція 3: живий бенчмарк (200 ітерацій) — порівняння часу звичайного LINQ проти `EF.CompileQuery`. |
| **8.2** | [Проєкція на представлення (Views)](8_2_DatabaseViews.md) · [код](../../src/08_AdvancedTopics/Lesson_8_2_DatabaseViews.cs) | • Навіщо `CREATE VIEW`: інкапсуляція складних `JOIN`/агрегацій на стороні БД.<br>• Мапінг в EF Core: `HasNoKey().ToView("View_ProductsByCompany")` (Keyless Entity Type).<br>• Keyless-сутності **ніколи не відстежуються** (`ChangeTracker` порожній) і працюють лише на читання, але поверх них повноцінно працює LINQ (`Where`, `OrderBy`). | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 8.2` | • Секція 2: `ChangeTracker.Entries().Count() == 0` навіть без `AsNoTracking()`. |
| **8.3** | [Зберігання історії змін](8_3_ChangeHistory.md) · [код](../../src/08_AdvancedTopics/Lesson_8_3_ChangeHistory.cs) | • Як це працює в **MS SQL Server**: вбудовані **Temporal Tables** (`ToTable(t => t.IsTemporal())`) та LINQ-методи `TemporalAll()`, `TemporalAsOf(date)`.<br>• **Адаптація для SQLite:** реалізація аудиту через `SaveChangesInterceptor` + таблицю `UserHistories` (або SQLite-тригери `AFTER INSERT/UPDATE/DELETE`, які додатково ловлять `ExecuteUpdate`/`ExecuteDelete`).<br>• Можливість повного відновлення видаленого запису зі збереженням початкового `Id`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 8.3` | • Повний журнал еволюції запису (`INSERT -> UPDATE -> UPDATE -> DELETE`) у `UserHistories` та успішне відновлення видаленого користувача в Секції 3. |

---

## Наскрізна перевірка всього курсу (фінал навчання)

```powershell
# Прогнати всі 58 уроків поспіль і переконатися, що кожен завершується зі статусом OK
dotnet run --project src/EFCoreMastery.ConsoleApp -- --all
```

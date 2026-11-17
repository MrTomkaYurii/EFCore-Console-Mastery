# 1.6. Логування операцій

- **Глава:** 01. Вступ до Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/1.6.php](https://metanit.com/sharp/efcore/1.6.php)
- **Код прикладу:** [`Lesson_1_6_Logging.cs`](../../src/01_Introduction/Lesson_1_6_Logging.cs)

## Мета уроку

Навчитися переглядати згенеровані SQL-запити та діагностичні повідомлення EF Core за допомогою методу `LogTo()`, а також фільтрувати логи за рівнем (`LogLevel`), ідентифікатором події (`RelationalEventId`) та категорією (`DbLoggerCategory`).

## Теоретичний матеріал

### Метод `LogTo`
Метод `DbContextOptionsBuilder.LogTo(...)` дозволяє перенаправити всі діагностичні повідомлення EF Core у будь-який приймач, що приймає делегат `Action<string>`:
- У консоль: `optionsBuilder.LogTo(Console.WriteLine)`
- У вікно Output в IDE: `optionsBuilder.LogTo(msg => Debug.WriteLine(msg))`
- У файл: через `StreamWriter` (із коректним закриттям потоку в перевизначених `Dispose()` / `DisposeAsync()`).

### Фільтрація логів

1. **За рівнем важливості (`LogLevel`):**
   - `Trace` — максимальна деталізація (лише для розробки).
   - `Debug` — рівень за замовчуванням для `LogTo`.
   - `Information` — потік виконання операцій, включно з виконаними SQL-командами.
   - `Warning`, `Error`, `Critical`, `None`.

2. **За ідентифікатором події (`EventId`):**
   - `CoreEventId` — загальні події ядра EF Core.
   - `RelationalEventId` — події реляційних БД (наприклад, `RelationalEventId.CommandExecuted` — лише виконані SQL-команди).
   - Специфічні класи провайдерів (наприклад, `SqliteEventId` для SQLite або `SqlServerEventId` для SQL Server).

3. **За категорією (`DbLoggerCategory`):**
   - `DbLoggerCategory.Database.Command.Name` — SQL-команди.
   - `DbLoggerCategory.Database.Connection.Name` — відкриття/закриття з'єднань.
   - `DbLoggerCategory.Database.Transaction.Name` — транзакції.
   - `DbLoggerCategory.Update.Name` — робота `SaveChanges()`.
   - Інші: `Migration`, `Model`, `Query`, `Scaffolding`, `Infrastructure`.

## Ключові фрагменти коду

Фільтрація за рівнем, подією та категорією:

```csharp
// 1. За рівнем LogLevel
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);

// 2. Тільки виконані SQL-команди (RelationalEventId)
optionsBuilder.LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuted });

// 3. За категорією DbLoggerCategory
optionsBuilder.LogTo(
    Console.WriteLine,
    new[] { DbLoggerCategory.Database.Command.Name },
    LogLevel.Information);
```

## Особливості роботи в SQLite

- Для подій, специфічних для SQLite (наприклад, попередження про обмеження міграцій чи типів), замість `SqlServerEventId` (як на Metanit) використовується `SqliteEventId` з простору імен `Microsoft.EntityFrameworkCore.Diagnostics`.
- У консольному логі SQLite ви побачите параметризований SQL з параметрами виду `@p0`, `@p1` та `RETURNING "Id"` після `INSERT INTO "Users" ...` (EF Core використовує `RETURNING` у сучасних версіях SQLite для отримання автоінкрементного первинного ключа).

## Підсумок та підводні камені

- За замовчуванням значення параметрів у логах приховані (`?`) з міркувань безпеки. Для локального налагодження можна додати `.EnableSensitiveDataLogging()`, але **ніколи не вмикайте** його в продакшені.
- Якщо ви логуєте у файл через `StreamWriter` усередині `DbContext`, не забудьте звільнити `StreamWriter` у `Dispose()` та `DisposeAsync()`, інакше файл залишиться заблокованим або дані не запишуться з буфера.
- Надмірне логування (`LogLevel.Trace` / `Debug`) у продакшені суттєво знижує продуктивність — фільтруйте лише потрібні категорії або рівні.

# Шпаргалка лектора: Глава 7. SQL в Entity Framework Core

> **Фокус глави:** коли LINQ недостатньо і потрібно писати чистий SQL, як захиститися від **SQL Injection**, а також як працювати зі збереженими функціями та процедурами (з урахуванням архітектурних відмінностей **SQLite** та **MS SQL Server**).

## План проведення та запуски

| ID | Тема та конспект | Про що говорити (ключові тези) | Команда запуску | На що звернути увагу у виводі |
|---|---|---|---|---|
| **6.1** | [Виконання SQL-запитів](6_01_RawSqlQueries.md) · [код](../../src/07_SqlAndStoredProcedures/Lesson_6_01_RawSqlQueries.cs) | • `FromSqlRaw` vs `FromSqlInterpolated` (автоматична параметризація — **захист від SQL Injection!**).<br>• Композиція: поверх `FromSqlRaw` можна чіпляти звичайні LINQ-методи (`OrderBy`, `Include`, `Where`).<br>• `Database.SqlQuery<T>` (EF Core 8+) — для скалярів і довільних DTO, які не є сутностями `DbSet`.<br>• `ExecuteSqlRaw` / `ExecuteSqlInterpolated` — для DML (`INSERT`/`UPDATE`/`DELETE`), повертають кількість змінених рядків. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 6.1` | • Показати всі 5 секцій: як `FromSqlRaw` комбінується з `Include`, як безпечно передаються параметри `SqliteParameter`, як працює `SqlQuery<T>` для DTO і як DML повертає `rowsAffected`. |
| **6.2** | [Збережені функції](6_02_StoredFunctions.md) · [код](../../src/07_SqlAndStoredProcedures/Lesson_6_02_StoredFunctions.cs) | • Навіщо мапити БД-функції через `HasDbFunction(...)` (можливість викликати їх прямо всередині LINQ-запитів).<br>• **Відмінність СУБД:** у MS SQL Server створюється `CREATE FUNCTION` (скалярна або TVF); у SQLite серверних функцій немає, тому функція реєструється на з'єднанні як **SQLite UDF** (`connection.CreateFunction(...)`).<br>• Прямий виклик функції у `FromSqlRaw`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 6.2` | • Секція 1: C#-метод, зареєстрований через `HasDbFunction`, транслюється в SQL-виклик UDF.<br>• Секція 3: зведене порівняння із синтаксисом MS SQL Server. |
| **6.3** | [Збережені процедури](6_03_StoredProcedures.md) · [код](../../src/07_SqlAndStoredProcedures/Lesson_6_03_StoredProcedures.cs) | • Як у MS SQL Server викликають `EXEC GetUsersByCompany @name` через `FromSqlRaw` і отримують `OUTPUT`-параметри (`ParameterDirection.Output`).<br>• **Чому в SQLite немає `CREATE PROCEDURE`:** SQLite — вбудована (in-process) бібліотека без серверного рушія.<br>• Практичні альтернативи в SQLite: параметризований `FromSqlInterpolated`, `Database.SqlQuery<string>` для скалярного результату та **явна транзакція** (`BeginTransaction()`) для багатокрокової атомарності. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 6.3` | • Порівняння обмежень SQLite та робочих патернів заміни збережених процедур, включно з атомарною транзакцією в Секції 4. |

---

## Важливі нагадування з безпеки (під час лекції)

| Патерн | Безпечно? | Пояснення для студентів |
|---|---|---|
| `FromSqlInterpolated($"SELECT * FROM Users WHERE Name = {name}")` | **Так** | EF Core перетворює `{name}` на `DbParameter` (`@p0`), а не склеює рядок |
| `FromSqlRaw("SELECT * FROM Users WHERE Name = {0}", name)` | **Так** | Позиційний параметр також передається як `DbParameter` |
| `FromSqlRaw($"SELECT * FROM Users WHERE Name = '{name}'")` | **НІ (SQL Injection!)** | C# спочатку інтерполює рядок до передачі в EF Core |

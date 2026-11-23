# Шпаргалка лектора: Глава 7. SQL в Entity Framework Core

> **Фокус глави:** коли LINQ недостатньо і потрібно писати чистий T-SQL, захист від **SQL Injection** (`FromSql` / `ExecuteSql`), а також нативні **збережені функції (TVF)** та **збережені процедури з `OUTPUT`-параметрами** у **MS SQL Server**.

## План проведення та запуски

| ID | Тема та конспект | Про що говорити (ключові тези) | Команда запуску | На що звернути увагу у виводі |
|---|---|---|---|---|
| **7.1** | [Виконання SQL-запитів](7_01_RawSqlQueries.md) · [код](../../src/07_SqlAndStoredProcedures/Lesson_7_01_RawSqlQueries.cs) | • `FromSqlRaw` vs сучасний `FromSql($"...")` (автоматична параметризація — **захист від SQL Injection!**).<br>• Композиція: поверх `FromSqlRaw` можна чіпляти `OrderBy`, `Include`, `Where`.<br>• `Database.SqlQuery<T>` (EF Core 8+) — для скалярів і довільних DTO.<br>• `ExecuteSql` — для DML (`UPDATE`/`DELETE`), повертає кількість змінених рядків. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 7.1` | • Усі 5 секцій на реальному MS SQL Server: `Include`, `SqlParameter`, `FromSql`, `SqlQuery<T>` для DTO та `ExecuteSql`. |
| **7.2** | [Збережені функції](7_02_StoredFunctions.md) · [код](../../src/07_SqlAndStoredProcedures/Lesson_7_02_StoredFunctions.cs) | • Реальна таблична функція (TVF) у MS SQL Server: `CREATE FUNCTION [l7_02].[GetUsersByAge](@age INT) RETURNS TABLE`.<br>• Мапінг через `HasDbFunction(...)` + `FromExpression(...)` — дозволяє викликати SQL-функцію прямо в LINQ з подальшим `OrderBy`/`Where`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 7.2` | • У `ToQueryString()` видно реальний виклик `FROM [l7_02].[GetUsersByAge](@age) AS [g] ORDER BY [g].[Name]`. |
| **7.3** | [Збережені процедури](7_03_StoredProcedures.md) · [код](../../src/07_SqlAndStoredProcedures/Lesson_7_03_StoredProcedures.cs) | • Реальні процедури `CREATE PROCEDURE [l7_03].[GetUsersByCompany]` та `GetUserWithMaxAge`.<br>• Виклик `EXEC` через `FromSqlRaw` (зверніть увагу: поверх `EXEC` не можна чіпляти LINQ `Where`/`OrderBy` на стороні SQL!).<br>• Отримання `OUTPUT`-параметра через `SqlParameter` з `ParameterDirection.Output`. | `dotnet run --project src/EFCoreMastery.ConsoleApp -- 7.3` | • Секція 1 повертає список користувачів Microsoft через `EXEC`; Секція 2 друкує значення `outParam.Value` з `OUTPUT`-параметра. |

---

## Важливі нагадування з безпеки (під час лекції)

| Патерн | Безпечно? | Пояснення для студентів |
|---|---|---|
| `FromSqlInterpolated($"SELECT * FROM Users WHERE Name = {name}")` | **Так** | EF Core перетворює `{name}` на `DbParameter` (`@p0`), а не склеює рядок |
| `FromSqlRaw("SELECT * FROM Users WHERE Name = {0}", name)` | **Так** | Позиційний параметр також передається як `DbParameter` |
| `FromSqlRaw($"SELECT * FROM Users WHERE Name = '{name}'")` | **НІ (SQL Injection!)** | C# спочатку інтерполює рядок до передачі в EF Core |

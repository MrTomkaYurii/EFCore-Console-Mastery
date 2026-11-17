# 6.3. Збережені процедури (обмеження SQLite та параметризований SQL)

- **Глава:** 07. SQL в Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/6.3.php](https://metanit.com/sharp/efcore/6.3.php)
- **Код прикладу:** [Lesson_6_3_StoredProcedures.cs](../../src/07_SqlAndStoredProcedures/Lesson_6_3_StoredProcedures.cs)

## Мета уроку

Розібратися, як у EF Core викликаються збережені процедури на прикладі MS SQL Server (вхідні та вихідні `OUTPUT`-параметри), зрозуміти принципове обмеження SQLite і навчитися реалізовувати еквівалентну логіку в SQLite через параметризований SQL і транзакції.

## Теоретичний матеріал

### Збережені процедури в MS SQL Server (за матеріалами Metanit)

У клієнт-серверних СУБД (MS SQL Server, PostgreSQL, Oracle) збережені процедури (`CREATE PROCEDURE`) дозволяють зберігати й виконувати на сервері БД цілі сценарії з параметрами, умовами та транзакціями.

#### 1. Процедура з вхідним параметром (повертає набір сутностей)

```sql
CREATE PROCEDURE [dbo].[GetUsersByCompany]
    @name nvarchar(50)
AS
    SELECT * FROM Users
    WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = @name)
GO
```

Виклик у EF Core через `FromSqlRaw`:

```csharp
SqlParameter param = new("@name", "Microsoft");
var users = db.Users.FromSqlRaw("GetUsersByCompany @name", param).ToList();
```

#### 2. Процедура з вихідним параметром (`OUTPUT`)

```sql
CREATE PROCEDURE [dbo].[GetUserWithMaxAge]
    @name varchar(50) OUTPUT
AS
    SELECT @name = [Name] FROM Users WHERE Age = (SELECT MAX(Age) FROM Users)
RETURN 0
```

Виклик у EF Core через `Database.ExecuteSqlRaw` із `ParameterDirection.Output`:

```csharp
SqlParameter param = new()
{
    ParameterName = "@userName",
    SqlDbType = System.Data.SqlDbType.VarChar,
    Direction = System.Data.ParameterDirection.Output,
    Size = 50
};
db.Database.ExecuteSqlRaw("GetUserWithMaxAge @userName OUT", param);
Console.WriteLine(param.Value);
```

### Чому SQLite не підтримує збережені процедури

SQLite — це **вбудована файлова бібліотека** (`in-process`), а не окремий сервер баз даних. Уся логіка застосунку виконується в тому ж процесі, що й доступ до файлу `.db`, тому в SQLite:
- відсутні `CREATE PROCEDURE` / `EXEC` / `CALL`;
- відсутні `OUTPUT`-параметри на рівні SQL-команд.

Натомість ті самі задачі (інкапсуляція запитів, параметризація, атомарність кількох DML-операцій) у SQLite вирішуються через:
1. **Параметризовані `FromSqlRaw` / `FromSqlInterpolated`** — для вибірки сутностей за параметром.
2. **`Database.SqlQuery<T>`** — для отримання скалярних значень (заміна `OUTPUT`-параметрів).
3. **Явні транзакції (`BeginTransaction`) + `ExecuteSqlInterpolated`** — для атомарного виконання кількох змін даних.

## Ключові фрагменти коду

### Еквівалент `GetUsersByCompany` у SQLite

```csharp
SqliteParameter companyParam = new("@name", "Microsoft");
var companyUsers = db.Users
    .FromSqlRaw(
        "SELECT * FROM Users WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = @name)",
        companyParam)
    .ToList();
```

### Еквівалент `OUTPUT`-параметра через `SqlQuery<string>`

```csharp
string? oldestUserName = db.Database
    .SqlQuery<string>($"""
        SELECT Name AS Value
        FROM Users
        WHERE Age = (SELECT MAX(Age) FROM Users)
        LIMIT 1
        """)
    .SingleOrDefault();
```

### Атомарна багатокрокова операція (заміна DML-процедури)

```csharp
using var transaction = db.Database.BeginTransaction();
try
{
    db.Database.ExecuteSqlInterpolated($"INSERT INTO Companies (Name) VALUES ({"OpenAI"})");
    db.Database.ExecuteSqlInterpolated(
        $"UPDATE Users SET Age = Age + {1} WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = {"Microsoft"})");
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

## Особливості роботи в SQLite

- Будь-яка спроба виконати `CREATE PROCEDURE` або `EXEC ...` у SQLite призведе до синтаксичної помилки `SqliteException: SQLite Error 1: 'near "PROCEDURE": syntax error'`.
- Використання `BeginTransaction()` у поєднанні з параметризованим SQL дає повний аналог транзакційної поведінки збереженої процедури без мережевих витрат (оскільки SQLite працює в тому ж процесі).

## Підсумок та підводні камені

- Для виклику збереженої процедури в SQL Server, яка повертає **рядки таблиці**, використовуйте `DbSet<T>.FromSqlRaw("ProcName @p", param)` (причому повернені стовпці мають відповідати сутності `T`, а до результату не можна докомпонувати LINQ `Where`/`OrderBy` поверх `EXEC`).
- Для процедури, що повертає значення лише через **`OUTPUT`-параметр** або не повертає таблицю, використовуйте `db.Database.ExecuteSqlRaw(...)` і не забувайте вказувати `Size` для рядкових `Output`-параметрів у `SqlParameter`.
- У проєктах, де важлива переносимість між SQLite (для локальних тестів) та SQL Server (для продакшену), уникайте жорсткої прив'язки до збережених процедур без абстракції сервісного шару.

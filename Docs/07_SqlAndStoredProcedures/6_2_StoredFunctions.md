# 6.2. Збережені функції (SQLite UDF / HasDbFunction адаптація)

- **Глава:** 07. SQL в Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/6.2.php](https://metanit.com/sharp/efcore/6.2.php)
- **Код прикладу:** [Lesson_6_2_StoredFunctions.cs](../../src/07_SqlAndStoredProcedures/Lesson_6_2_StoredFunctions.cs)

## Мета уроку

Зрозуміти, як EF Core проєктує функції бази даних на методи C# за допомогою `HasDbFunction` та `FromExpression`, а також навчитися використовувати користувацькі функції (UDF) у SQLite через `SqliteConnection.CreateFunction`.

## Теоретичний матеріал

У клієнт-серверних СУБД (зокрема MS SQL Server, розглянутому на Metanit) бізнес-логіку можна інкапсулювати у **збережені функції** (`CREATE FUNCTION`):
- **Скалярні функції** — повертають одне значення (число, рядок, булеве значення).
- **Табличні функції (Table-Valued Functions, TVF)** — повертають таблицю рядків.

EF Core дозволяє працювати з функціями БД двома способами:
1. **Безпосередньо в сирому SQL** (`FromSqlRaw` / `FromSqlInterpolated`).
2. **Проєкція на метод C# через `HasDbFunction`** — метод реєструється в `OnModelCreating`, після чого його можна викликати всередині звичайних LINQ-запитів, а EF Core транслюватиме виклик у SQL.

### Класичний підхід для MS SQL Server (Metanit)

У MS SQL Server створюється таблична функція:

```sql
CREATE FUNCTION [dbo].[GetUsersByAge] (@age int)
RETURNS @returntable TABLE (Id int, CompanyId int, Name nvarchar(50), Age int)
AS
BEGIN
    INSERT @returntable
    SELECT Id, CompanyId, Name, Age FROM Users WHERE Age < @age
    RETURN
END
```

У `DbContext` оголошується метод екземпляра з `FromExpression` і реєструється через `HasDbFunction`:

```csharp
public IQueryable<User> GetUsersByAge(int age)
    => FromExpression(() => GetUsersByAge(age));

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDbFunction(() => GetUsersByAge(default));
}
```

### Адаптація для SQLite: користувацькі UDF (`CreateFunction`)

SQLite не має DDL-синтаксису `CREATE FUNCTION` у SQL, але драйвер `Microsoft.Data.Sqlite` дозволяє зареєструвати будь-який C#-делегат як скалярну SQL-функцію (User-Defined Function, UDF) безпосередньо на відкритому `SqliteConnection`:

```csharp
connection.CreateFunction<int, int, bool>(
    "is_senior_user",
    (age, threshold) => age >= threshold,
    isDeterministic: true);
```

Після реєстрації функцію можна викликати як у сирому SQL, так і через `HasDbFunction` у LINQ!

## Ключові фрагменти коду

### Оголошення C#-заглушок і реєстрація `HasDbFunction`

```csharp
public static bool IsSeniorUser(int age, int threshold)
    => throw new NotSupportedException("Призначено лише для трансляції в SQL.");

public static int SqliteLength(string? text)
    => throw new NotSupportedException("Призначено лише для трансляції в SQL.");

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDbFunction(
            typeof(ApplicationContext).GetMethod(nameof(IsSeniorUser), new[] { typeof(int), typeof(int) })!)
        .HasName("is_senior_user");

    // Мапінг вбудованої функції SQLite length()
    modelBuilder.HasDbFunction(
            typeof(ApplicationContext).GetMethod(nameof(SqliteLength), new[] { typeof(string) })!)
        .HasName("length");
}
```

### Використання в LINQ та `FromSqlRaw`

```csharp
// Через LINQ + HasDbFunction (EF Core транслює в SELECT ... WHERE is_senior_user("u"."Age", @__threshold_0))
var seniorBadges = db.Users
    .Where(u => ApplicationContext.IsSeniorUser(u.Age, 30))
    .Select(u => new
    {
        Badge = ApplicationContext.FormatUserBadge(u.Name ?? "Unknown", u.Age),
        NameLength = ApplicationContext.SqliteLength(u.Name)
    })
    .ToList();

// Безпосередньо в сирому SQL
var youngerUsers = db.Users
    .FromSqlRaw("SELECT * FROM Users WHERE is_senior_user(Age, {0}) = 0", 30)
    .ToList();
```

## Особливості роботи в SQLite

| Аспект | MS SQL Server (Metanit) | SQLite (цей проєкт) |
| :--- | :--- | :--- |
| Створення функції в БД | `CREATE FUNCTION` у T-SQL | Не підтримується в SQL; використовується `SqliteConnection.CreateFunction` у C# |
| Табличні функції (TVF) | Повністю підтримуються (`FromExpression`) | Не підтримуються |
| Скалярні функції | `CREATE FUNCTION ... RETURNS <type>` | `CreateFunction(...)` або вбудовані (`length`, `upper`, `lower`, `substr`) |
| Прив'язка з'єднання | Рядок підключення | Потрібно передавати відкритий `SqliteConnection` в `UseSqlite(connection)` |

## Підсумок та підводні камені

- Статичні методи-заглушки для `HasDbFunction` **не повинні** викликатися у звичайному C#-коді поза LINQ-виразами до `DbSet` (інакше спрацює `NotSupportedException`).
- `SqliteConnection.CreateFunction` діє **лише в межах конкретного екземпляра `SqliteConnection`**, тому з'єднання має залишатися відкритим на час роботи `DbContext`.
- Якщо UDF залежить лише від своїх аргументів (без звернення до поточного часу чи випадкових чисел), вказуйте `isDeterministic: true` — це дозволяє SQLite краще оптимізувати запити та використовувати індекси.

# 6.1. Виконання SQL-запитів (FromSqlRaw, FromSqlInterpolated, SqlQuery, ExecuteSqlRaw)

- **Глава:** 07. SQL в Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/6.1.php](https://metanit.com/sharp/efcore/6.1.php)
- **Код прикладу:** [Lesson_6_1_RawSqlQueries.cs](../../src/07_SqlAndStoredProcedures/Lesson_6_1_RawSqlQueries.cs)

## Мета уроку

Навчитися виконувати сирі SQL-запити в Entity Framework Core для читання сутностей, скалярних значень і довільних DTO, а також для модифікації даних (INSERT, UPDATE, DELETE) із безпечною параметризацією.

## Теоретичний матеріал

Попри потужність LINQ to Entities, інколи доцільно писати SQL вручну — наприклад, для дуже складних запитів, специфічних конструкцій СУБД або оптимізації вузьких місць. EF Core надає для цього кілька методів, які поділяються на **запити вибірки** та **команди модифікації даних**.

### Порівняння методів виконання SQL у EF Core

| Метод | Призначення | Повертає | Підтримка композиції LINQ | Захист від SQL-ін'єкцій |
| :--- | :--- | :--- | :--- | :--- |
| `DbSet.FromSqlRaw` | Вибірка сутностей моделі | `IQueryable<TEntity>` | Так (`Where`, `OrderBy`, `Include`) | Через `DbParameter` або `{0}` |
| `DbSet.FromSqlInterpolated` | Вибірка сутностей через `FormattableString` | `IQueryable<TEntity>` | Так | Автоматична параметризація (`@p0`, `@p1`) |
| `Database.SqlQuery<T>` | Вибірка скалярів або довільних DTO (без `DbSet`) | `IQueryable<T>` | Так (у EF Core 8+) | Автоматична параметризація |
| `Database.ExecuteSqlRaw` | DML-команди (`INSERT`, `UPDATE`, `DELETE`) | `int` (кількість змінених рядків) | Ні | Через `DbParameter` або `{0}` |
| `Database.ExecuteSqlInterpolated` | DML-команди через інтерполяцію | `int` (кількість змінених рядків) | Ні | Автоматична параметризація |

### Правила та обмеження `FromSqlRaw` / `FromSqlInterpolated`

- Запит повинен повертати дані, що відповідають структурі сутності `TEntity` (усі обов'язкові стовпці таблиці).
- Сам SQL-вираз **не повинен** намагатися завантажити пов'язані дані через `JOIN` для навігаційних властивостей — для цього використовуйте `.Include(...)` поверх `FromSqlRaw`.
- Оскільки `FromSqlRaw` та `FromSqlInterpolated` повертають `IQueryable<TEntity>`, до них можна додавати LINQ-оператори (`OrderBy`, `Where`, `Skip`, `Take`), які EF Core об'єднає в один підзапит SQL.

### `Database.SqlQuery<T>` для скалярів і DTO

Починаючи з EF Core 8, `db.Database.SqlQuery<T>(FormattableString)` дозволяє отримувати:
- **скалярні значення** (`int`, `string`, `DateTime` тощо) — за умови, що результуючий стовпець має ім'я `Value` (наприклад, `SELECT COUNT(*) AS Value FROM Users`);
- **довільні DTO-класи**, які не зареєстровані в `DbContext` як `DbSet<T>`.

## Ключові фрагменти коду

### Композиція `FromSqlRaw` з `OrderBy` та `Include`

```csharp
var companies = db.Companies
    .FromSqlRaw("SELECT * FROM Companies")
    .OrderBy(c => c.Name)
    .ToList();

var usersWithCompany = db.Users
    .FromSqlRaw("SELECT * FROM Users")
    .Include(u => u.Company)
    .ToList();
```

### Безпечна передача параметрів

```csharp
// 1. Через SqliteParameter
SqliteParameter nameParam = new("@name", "%Tom%");
var tomUsers = db.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Name LIKE @name", nameParam)
    .ToList();

// 2. Через позиційний плейсхолдер {0}
var olderUsers = db.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Age > {0}", 30)
    .ToList();

// 3. Через FromSqlInterpolated (створює @p0, @p1 автоматично)
string pattern = "%Tom%";
int ageLimit = 30;
var filtered = db.Users
    .FromSqlInterpolated($"SELECT * FROM Users WHERE Name LIKE {pattern} AND Age > {ageLimit}")
    .ToList();
```

### Вибірка скалярів і DTO через `SqlQuery<T>`

```csharp
int totalUsers = db.Database
    .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Users")
    .Single();

var stats = db.Database
    .SqlQuery<CompanyStatDto>($"""
        SELECT c.Name AS CompanyName, COUNT(u.Id) AS UserCount, COALESCE(AVG(u.Age), 0.0) AS AverageAge
        FROM Companies c
        LEFT JOIN Users u ON u.CompanyId = c.Id
        GROUP BY c.Name
        """)
    .ToList();
```

### Модифікація даних через `ExecuteSqlRaw` та `ExecuteSqlInterpolated`

```csharp
int inserted = db.Database.ExecuteSqlRaw("INSERT INTO Companies (Name) VALUES ({0})", "Apple");
int updated = db.Database.ExecuteSqlRaw("UPDATE Companies SET Name = {0} WHERE Name = {1}", "Apple Inc.", "Apple");
int deleted = db.Database.ExecuteSqlRaw("DELETE FROM Companies WHERE Name = {0}", "Apple Inc.");

string jetbrains = "JetBrains";
db.Database.ExecuteSqlInterpolated($"INSERT INTO Companies (Name) VALUES ({jetbrains})");
```

Для асинхронного виконання доступні відповідні методи `ExecuteSqlRawAsync` та `ExecuteSqlInterpolatedAsync`.

## Особливості роботи в SQLite

- Для явних параметрів у `FromSqlRaw` / `ExecuteSqlRaw` у SQLite використовується клас **`SqliteParameter`** з простору імен `Microsoft.Data.Sqlite` (на відміну від `SqlParameter` з `Microsoft.Data.SqlClient` для MS SQL Server).
- Оператор `LIKE` у SQLite є **регістронезалежним для ASCII-символів** за замовчуванням, але регістрозалежним для Unicode (кирилиці).
- Функція `AVG(...)` у SQLite повертає `NULL`, якщо вибірка порожня, тому для безпечного мапінгу в `double` використано `COALESCE(AVG(u.Age), 0.0)`.

## Підсумок та підводні камені

- **Ніколи не конкатенуйте рядки** у `FromSqlRaw($"... {var}")` напряму через звичайну змінну типу `string` — це відкриває шлях до SQL-ін'єкцій! Завжди передавайте `FormattableString` у `FromSqlInterpolated` / `ExecuteSqlInterpolated` або використовуйте параметри `{0}` / `SqliteParameter`.
- При композиції LINQ поверх `FromSqlRaw` сам SQL-рядок **не повинен** закінчуватися крапкою з комою `;`, інакше обгортка `FROM (...) AS "c"` викличе синтаксичну помилку SQL.
- `ExecuteSqlRaw` / `ExecuteSqlInterpolated` **не оновлюють** уже відстежувані (`Tracked`) сутності в `ChangeTracker`. Якщо сутності вже завантажені в контекст, після масового `UPDATE`/`DELETE` варто очистити трекер або завантажити дані заново.

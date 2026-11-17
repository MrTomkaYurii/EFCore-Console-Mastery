# 8.2. Проєкція запитів на представлення (Views / ToView)

- **Глава:** 08. Додаткові теми
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/8.2.php](https://metanit.com/sharp/efcore/8.2.php)
- **Код прикладу:** [Lesson_8_2_DatabaseViews.cs](../../src/08_AdvancedTopics/Lesson_8_2_DatabaseViews.cs)

---

## Мета уроку

Навчитися зіставляти `DbSet<T>` у контексті даних із SQL-представленнями (`VIEW`) бази даних за допомогою методів `HasNoKey()` і `ToView(...)`, а також виконувати поверх них типізовані LINQ-запити.

---

## Теоретичний матеріал

Окрім звичайних таблиць, EF Core дозволяє проєктувати дані з **представлень бази даних (Views)** на класи C#. Це особливо зручно для складних агрегованих вибірок, звітів та об'єднань (`JOIN` + `GROUP BY`), які інкапсульовані на рівні SQL.

### Обмеження типів, зіставлених з представленнями (`Keyless Entity Types`)

1. **Без первинного ключа:** для такого класу зазвичай викликають `HasNoKey()`.
2. **Не відстежуються контекстом:** об'єкти, отримані з такого `DbSet`, завжди перебувають у стані `EntityState.Detached` (`ChangeTracker` їх не відстежує).
3. **Тільки для читання:** ми можемо отримувати дані, фільтрувати та сортувати їх через LINQ, але **не можемо** додавати (`Add`), оновлювати (`Update`) чи видаляти (`Remove`) через `SaveChanges()`.
4. **Навігаційні властивості:** звичайні сутності-таблиці не можуть мати навігаційних властивостей на безключові типи (хоча сам безключовий тип може мати посилання на звичайну сутність).

### Навіщо потрібен `ToView(...)`?

Виклик `modelBuilder.Entity<T>().ToView("ViewName")` повідомляє EF Core про дві речі:
- Під час вибірки потрібно робити `SELECT ... FROM ViewName`.
- Під час `EnsureCreated()` або генерації міграцій EF Core **не повинен** створювати фізичну таблицю `ViewName` (оскільки саме SQL-представлення створюється окремим SQL-скриптом або у міграції через `migrationBuilder.Sql(...)`).

---

## Ключові фрагменти коду

Конфігурація безключової сутності для представлення в `OnModelCreating`:

```csharp
modelBuilder.Entity<CompanyProductsGroup>(pc =>
{
    pc.HasNoKey();
    pc.ToView("View_ProductsByCompany");
});
```

Створення SQL-представлення у SQLite та звернення до нього через LINQ:

```csharp
setupDb.Database.ExecuteSqlRaw("""
    CREATE VIEW IF NOT EXISTS View_ProductsByCompany AS
    SELECT
        c.Name AS CompanyName,
        COUNT(p.Id) AS ProductCount,
        SUM(p.Price * p.TotalCount) AS TotalSum
    FROM Companies c
    INNER JOIN Products p ON p.CompanyId = c.Id
    GROUP BY c.Name;
    """);

// Вибірка з фільтрацією через LINQ:
var topCompanies = db.ProductsByCompany
    .Where(g => g.TotalSum > 150_000)
    .OrderByDescending(g => g.TotalSum)
    .ToList();
```

---

## Особливості роботи в SQLite

- **Синтаксис `CREATE VIEW`:** SQLite повністю підтримує стандартні SQL-представлення (`CREATE VIEW ... AS SELECT ...`), включаючи `CREATE VIEW IF NOT EXISTS`.
- **EnsureCreated():** як і в SQL Server, `Database.EnsureCreated()` у SQLite створює лише таблиці, але **не створює** `VIEW`. Тому в навчальному прикладі ми створюємо представлення одразу після `RecreateDatabase()` за допомогою `ExecuteSqlRaw(...)`. У реальних проєктах для цього використовують EF Core Migrations (`migrationBuilder.Sql(...)`).
- **Обмеження `ALTER VIEW` у SQLite:** на відміну від SQL Server (`CREATE OR ALTER VIEW`), SQLite не має `ALTER VIEW` — щоб змінити існуюче представлення в міграції, потрібно виконати `DROP VIEW IF EXISTS ...; CREATE VIEW ...;`.

---

## Підсумок та підводні камені

- **Збіг імен стовпців:** імена псевдонімів (`AS CompanyName`, `AS ProductCount`, `AS TotalSum`) у `SELECT` представлення повинні точно збігатися з іменами властивостей C#-класу (або бути налаштовані через `HasColumnName(...)`).
- **Не намагайтеся змінювати дані через `ToView`:** навіть якщо `VIEW` у БД теоретично оновлюване, безключова сутність (`HasNoKey`) в EF Core призначена лише для читання.
- **Один клас — таблиця і представлення одночасно:** за потреби одну й ту саму сутність можна зіставити і з таблицею (`ToTable`), і з представленням (`ToView`), але для звітних проєкцій краще заводити окремий DTO/модельний клас.

# 5.1. Вступ до LINQ to Entities

- **Глава:** 06. Запити та LINQ to Entities
- **Оригінал на Metanit:** [Введение в LINQ to Entities](https://metanit.com/sharp/efcore/5.1.php)
- **Код прикладу:** [Lesson_5_1_LinqIntro.cs](../../src/06_QueriesAndLinq/Lesson_5_1_LinqIntro.cs)

## Мета уроку

Ознайомитися з технологією **LINQ to Entities**, зрозуміти, як EF Core перетворює C#-вирази на SQL-запити, та навчитися будувати запити двома способами: через **оператори LINQ** (Query Syntax) і через **методи розширення** (Method Syntax).

## Теоретичний матеріал

### Що таке LINQ to Entities
Реляційні бази даних (SQLite, SQL Server, PostgreSQL) розуміють лише мову SQL. **LINQ to Entities** — це провайдер запитів у складі Entity Framework Core, який приймає дерево виразів C# (`IQueryable<T>`) та транслює його у відповідний SQL-діалект вибраної СУБД.

### Два способи побудови запитів

| Спосіб | Синтаксис | Коли зручніше використовувати |
| :--- | :--- | :--- |
| **Оператори LINQ (Query Syntax)** | `from u in db.Users where ... select u` | Складні запити з кількома `join` або `let` |
| **Методи розширення (Method Syntax)** | `db.Users.Where(...).Include(...)` | Більшість повсякденних запитів, динамічна фільтрація, виклик методів на кшталт `Count()`, `FirstOrDefault()`, `AsNoTracking()` |

Обидва підходи компілюються в ідентичні дерева виразів і генерують однаковий SQL.

### Основні методи LINQ to Entities
- **Фільтрація та пошук:** `Where`, `First` / `FirstOrDefault`, `Single` / `SingleOrDefault`, `Find` / `FindAsync`.
- **Сортування та проекція:** `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `Select`.
- **З'єднання та групування:** `Join`, `GroupBy`, `Include` / `ThenInclude`.
- **Операції з множинами:** `Union`, `Intersect`, `Except`.
- **Агрегати:** `Any`, `All`, `Count`, `Sum`, `Min`, `Max`, `Average`.
- **Керування виконанням:** `ToList` / `ToListAsync`, `ToArray`, `AsNoTracking`, `IgnoreQueryFilters`.

## Ключові фрагменти коду

```csharp
// 1. Query Syntax (оператори LINQ)
var query1 = from user in db.Users.Include(u => u.Company)
             where user.CompanyId == 1
             select user;

// 2. Method Syntax (методи розширення)
var query2 = db.Users
    .Include(u => u.Company)
    .Where(u => u.CompanyId == 1);
```

Обидва запити транслюються в один і той самий SQL:

```sql
SELECT "u"."Id", "u"."Age", "u"."CompanyId", "u"."Name", "c"."Id", "c"."Name"
FROM "Users" AS "u"
INNER JOIN "Companies" AS "c" ON "u"."CompanyId" = "c"."Id"
WHERE "u"."CompanyId" = 1
```

## Особливості роботи в SQLite

- У SQLite ідентифікатори таблиць і стовпців екрануються подвійними лапками (`"Users"`, `"CompanyId"`), тоді як у SQL Server використовуються квадратні дужки (`[Users]`, `[CompanyId]`).
- EF Core автоматично враховує особливості провайдера `Microsoft.EntityFrameworkCore.Sqlite`, тому той самий LINQ-код працює без змін.

## Підсумок та підводні камені

- Поки не викликано матеріалізуючий метод (`ToList()`, `FirstOrDefault()`, `Count()` тощо), SQL-запит **не виконується**.
- Деякі складні C#-методи всередині `Where` або `Select` не можуть бути перекладені в SQL і викликають виняток `InvalidOperationException` під час виконання запиту.
- Для перегляду згенерованого SQL безпосередньо в коді зручно використовувати метод `.ToQueryString()`.

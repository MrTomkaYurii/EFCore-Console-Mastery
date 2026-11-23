# 5.1. Вступ до LINQ to Entities

- **Глава:** 06. Запити та LINQ to Entities
- **Джерело Metanit:** [Введение в LINQ to Entities](https://metanit.com/sharp/efcore/5.1.php)
- **Код прикладу:** [Lesson_5_1_LinqIntro.cs](../../src/06_QueriesAndLinq/Lesson_5_1_LinqIntro.cs)
- **Запуск уроку:** `dotnet run --project src/EFCoreMastery.ConsoleApp -- 5.1`
- **Шпаргалка глави:** [Runs.md](Runs.md)

## Мета уроку

Ознайомитися з технологією **LINQ to Entities**, зрозуміти механізм трансляції дерев виразів C# (`Expression<TDelegate>`) у SQL-запити конкретної СУБД (SQLite, MS SQL Server, PostgreSQL), а також навчитися формувати запити двома еквівалентними способами: за допомогою **операторів запиту LINQ** (Query Syntax) та **методів розширення LINQ** (Method Syntax).

## Детальний теоретичний матеріал

### Що таке LINQ to Entities

Реляційні бази даних (SQLite, SQL Server, PostgreSQL) розуміють виключно мову SQL. Натомість у .NET-застосунках розробник працює з об'єктами C#, класами сутностей і властивостями `DbSet<T>`. Для подолання цього розриву Entity Framework Core використовує **LINQ to Entities** — спеціалізований провайдер запитів, побудований на основі LINQ (Language Integrated Query).

Коли ви пишете запит до `DbSet<User>`, компілятор C# не виконує фільтрацію в пам'яті одразу. Оскільки `DbSet<T>` реалізує інтерфейс `IQueryable<T>`, усі лямбда-вирази передаються у вигляді **дерева виразів (Expression Tree)**. Під час матеріалізації запиту провайдер EF Core (наприклад, `Microsoft.EntityFrameworkCore.Sqlite`) обходить це дерево, оптимізує його та генерує нативний SQL-запит із параметрами.

### Два способи побудови запитів

У C# та LINQ to Entities існують два синтаксиси для опису того самого запиту:

| Критерій | Оператори LINQ (Query Syntax) | Методи розширення (Method Syntax) |
| :--- | :--- | :--- |
| **Зовнішній вигляд** | Декларативний SQL-подібний синтаксис (`from ... where ... select`) | Ланцюжок викликів методів (`db.Users.Where(...).Include(...)`) |
| **Компіляція** | Компілятор C# перетворює Query Syntax у виклики методів розширення | Безпосередній виклик методів класу `Queryable` та `EntityFrameworkQueryableExtensions` |
| **Згенерований SQL** | Ідентичний | Ідентичний |
| **Підтримка операцій** | Обмежена частина операторів (немає `Include`, `Count`, `FirstOrDefault`, `AsNoTracking` у вигляді ключових слів) | Повний набір методів LINQ та специфічних методів EF Core |
| **Коли доречніше** | Складні запити з кількома `join`, `let`, перехресними вибірками | Більшість реальних сценаріїв, динамічна фільтрація, пагінація |

На практиці ці підходи часто комбінують: наприклад, базовий запит описують через `from ... where ... select`, а до результату застосовують `.ToList()`, `.FirstOrDefault()` або `.Include(...)`.

### Повний перелік основних методів LINQ to Entities

Згідно з керівництвом Metanit, у EF Core найчастіше використовуються такі методи (багато з них мають синхронну та асинхронну версії):

| Категорія | Методи | Призначення |
| :--- | :--- | :--- |
| **Перевірка умов** | `All` / `AllAsync`, `Any` / `AnyAsync`, `Contains` / `ContainsAsync` | Перевірка, чи всі / хоча б один / конкретний елемент задовольняють умову |
| **Пошук одного елемента** | `First` / `FirstAsync`, `FirstOrDefault` / `FirstOrDefaultAsync`, `Single` / `SingleAsync`, `SingleOrDefault` / `SingleOrDefaultAsync`, `Find` / `FindAsync` | Отримання одного запису з БД (або `null`, якщо не знайдено) |
| **Агрегація** | `Count` / `CountAsync`, `LongCount`, `Sum` / `SumAsync`, `Min` / `MinAsync`, `Max` / `MaxAsync`, `Average` / `AverageAsync` | Обчислення підсумкових значень на стороні БД |
| **Фільтрація та зрізи** | `Where`, `Skip`, `Take` | Відбір рядків за умовою та реалізація пагінації |
| **Проекція та сортування** | `Select`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending` | Вибір потрібних стовпців/DTO та впорядкування результату |
| **З'єднання та групування** | `Join`, `GroupBy`, `Include`, `ThenInclude` | Поєднання таблиць і завантаження пов'язаних сутностей |
| **Операції з множинами** | `Union`, `Concat`, `Intersect`, `Except`, `Distinct` | Об'єднання, перетин, різниця та унікалізація вибірок |
| **Матеріалізація** | `ToList` / `ToListAsync`, `ToArray` / `ToArrayAsync`, `ToDictionary` / `ToDictionaryAsync` | Негайне виконання SQL-запиту та завантаження даних у пам'ять |
| **Керування трекінгом і фільтрами** | `AsNoTracking`, `AsNoTrackingWithIdentityResolution`, `IgnoreQueryFilters`, `ToQueryString` | Оптимізація продуктивності та діагностика SQL |

### Налагодження та перегляд SQL через `ToQueryString()`

Починаючи з EF Core 5, будь-який `IQueryable<T>` має метод розширення `ToQueryString()`. Він **не виконує** запит у базі даних, а лише запускає конвеєр трансляції LINQ -> SQL і повертає точний рядок SQL-команди з оголошенням параметрів. Це найзручніший спосіб перевірити, чи ефективно транслюється ваш LINQ-вираз.

## Діаграма / Архітектура процесу

```mermaid
flowchart LR
    A["C# код (Query або Method Syntax)"] --> B["IQueryable&lt;T&gt; + Expression Tree"]
    B --> C["EF Core Query Compiler"]
    C --> D["SQLite Query SqlGenerator"]
    D --> E["SQL-запит із параметрами"]
    E --> F["Виконання в SQLite (при ToList або foreach)"]
    F --> G["Матеріалізація сутностей + ChangeTracker"]
```

## Покроковий розбір прикладів коду

У файлі [Lesson_5_1_LinqIntro.cs](../../src/06_QueriesAndLinq/Lesson_5_1_LinqIntro.cs) змодельовано зв'язок один-до-багатьох між `Company` (головна сутність) та `User` (залежна сутність із зовнішнім ключем `CompanyId`).

### Крок 1. Ініціалізація та наповнення бази даних
Метод `SqliteDbHelper.RecreateDatabase(db)` гарантує чистий стан БД для кожного запуску уроку (`EnsureDeleted()` + `EnsureCreated()`). Далі створюються дві компанії (`Microsoft` з `Id = 1`, `Google` з `Id = 2`) та чотири користувачі (`Tom`, `Bob`, `Alice`, `Kate`), які зберігаються через `db.SaveChanges()`.

### Крок 2. Запит через Query Syntax та діагностика SQL
```csharp
var query = from user in db.Users.Include(u => u.Company)
            where user.CompanyId == 1
            select user;

Console.WriteLine(query.ToQueryString());
var users = query.ToList();
```
1. `db.Users.Include(u => u.Company)` вказує EF Core завантажити зв'язану компанію через `JOIN`.
2. `where user.CompanyId == 1` додає умову фільтрації.
3. До виклику `query.ToList()` запит до SQLite ще не надсилався — `query` є лише об'єктом `IQueryable<User>`.
4. `query.ToQueryString()` повертає такий SQLite-запит:
```sql
SELECT "u"."Id", "u"."Age", "u"."CompanyId", "u"."Name", "c"."Id", "c"."Name"
FROM "Users" AS "u"
INNER JOIN "Companies" AS "c" ON "u"."CompanyId" = "c"."Id"
WHERE "u"."CompanyId" = 1
```
5. `query.ToList()` виконує SQL-запит і повертає `Tom (36) - Microsoft` та `Alice (28) - Microsoft`.

### Крок 3. Еквівалентний запит через Method Syntax
```csharp
var query = db.Users
    .Include(u => u.Company)
    .Where(u => u.CompanyId == 1);

var users = query.ToList();
```
Цей запит формує точно таке саме дерево виразів і транслюється в ідентичний SQL-код із `INNER JOIN`. Зверніть увагу, що оскільки зовнішній ключ `CompanyId` має тип `int` (не nullable), EF Core оптимізує `Include` і генерує `INNER JOIN` замість `LEFT JOIN`.

## Особливості роботи в SQLite

| Аспект | SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) | MS SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`) |
| :--- | :--- | :--- |
| **Екранування ідентифікаторів** | Подвійні лапки: `"Users"`, `"CompanyId"` | Квадратні дужки: `[Users]`, `[CompanyId]` |
| **Типи даних у таблиці** | Динамічна типізація (`INTEGER`, `TEXT`, `REAL`, `BLOB`) | Сувора типізація (`int`, `nvarchar`, `datetime2`) |
| **Параметри запиту** | Підтримує іменовані параметри (`@__p_0`), які підставляються безпечно | Підтримує `@__p_0` і планування запитів на сервері |
| **Провайдерні обмеження** | Немає деяких серверних функцій (наприклад, окремі типи `DateTimeOffset` операцій або специфічні `APPLY` конструкції) | Повна підтримка складних конструкцій T-SQL (`CROSS APPLY`, `OFFSET/FETCH`) |

## Типові помилки, підводні камені та Best Practices

1. **Виклик неперекладних C#-методів усередині запиту.** Якщо у `Where` або `Select` викликати власний метод C# (наприклад, `Where(u => MyHelper.Check(u.Name))`), EF Core не зможе перетворити його на SQL і кине `InvalidOperationException` під час виконання.
2. **Плутанина між моментом створення запиту і моментом його виконання.** `var query = db.Users.Where(...)` лише описує запит. Реальне звернення до SQLite відбувається під час `ToList()`, `FirstOrDefault()`, `Count()` або перебору в `foreach`.
3. **Використовуйте `ToQueryString()` під час розробки.** Це допомагає вчасно помітити зайві `JOIN`, відсутність `WHERE` або небажані підзапити.
4. **Віддавайте перевагу асинхронним методам (`ToListAsync`, `FirstOrDefaultAsync`) у реальних веб- і десктопних застосунках**, щоб не блокувати потік на час очікування відповіді від БД.

---
<!-- lesson-nav -->
← [4.3. Підхід TPC - Table Per Class](../05_Inheritance/4_3_TpcInheritance.md) | [Шпаргалка глави (Runs.md)](Runs.md) | [Зміст курсу (README)](../../README.md) | [5.2. Вибірка та фільтрація (Where, EF.Functions.Like, Find)](5_2_Filtering.md) →

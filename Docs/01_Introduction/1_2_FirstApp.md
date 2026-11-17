# 1.2. Перший застосунок на EF Core

- **Глава:** 01. Вступ до Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/1.2.php](https://metanit.com/sharp/efcore/1.2.php)
- **Код прикладу:** [`Lesson_1_2_FirstApp.cs`](../../src/01_Introduction/Lesson_1_2_FirstApp.cs)

## Мета уроку

Створити мінімальний працездатний застосунок на EF Core: визначити клас моделі, налаштувати контекст `DbContext` із `DbSet<T>` та `OnConfiguring`, створити базу даних SQLite, додати записи й прочитати їх назад.

## Теоретичний матеріал

### Необхідні кроки для старту
1. Додати в проєкт NuGet-пакет провайдера конкретної СУБД — у нашому випадку `Microsoft.EntityFrameworkCore.Sqlite`.
2. Описати клас моделі (POCO — *Plain Old CLR Object*), який представляє таблицю в БД.
3. Створити клас контексту даних, успадкований від `Microsoft.EntityFrameworkCore.DbContext`.

### Основні класи простору імен `Microsoft.EntityFrameworkCore`
- **`DbContext`** — центральний клас, що керує з'єднанням із БД, відстеженням змін (*Change Tracker*) та виконанням запитів. Реалізує `IDisposable` та `IAsyncDisposable`, тому його варто обгортати в `using`.
- **`DbSet<TEntity>`** — колекція сутностей певного типу, через яку виконуються запити та операції додавання/видалення для відповідної таблиці.
- **`DbContextOptionsBuilder`** — конфігуратор параметрів підключення (вибір провайдера, рядка підключення, логування тощо).

### Конвенція первинного ключа та Nullable Reference Types
- За замовчуванням EF Core шукає властивість з іменем `Id` або `<Ім'яКласу>Id` (наприклад, `UserId`) і робить її первинним ключем таблиці.
- Коли в проєкті увімкнено `<Nullable>enable</Nullable>`, неініціалізована властивість `public DbSet<User> Users { get; set; }` викликає попередження компілятора `CS8618`. Це вирішують двома способами:
  - `public DbSet<User> Users => Set<User>();` (рекомендований спосіб без `null!`), або
  - `public DbSet<User> Users { get; set; } = null!;`.

## Ключові фрагменти коду

Модель та контекст даних:

```csharp
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }
}

public class ApplicationContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=helloapp.db");
    }
}
```

Додавання та вибірка об'єктів:

```csharp
using var db = new ApplicationContext();
var tom = new User { Name = "Tom", Age = 33 };
var alice = new User { Name = "Alice", Age = 26 };

db.Users.Add(tom);
db.Users.Add(alice);
db.SaveChanges();

var users = db.Users.ToList();
```

## Особливості роботи в SQLite

- Якщо в `UseSqlite("Data Source=helloapp.db")` вказати відносний шлях, файл БД створюється в поточній робочій директорії застосунку (або біля `.exe`). У цьому навчальному проєкті всі БД ізольовано створюються в папці `Databases/` через `SqliteDbHelper.GetConnectionString(...)`.
- На відміну від SQL Server, SQLite не потребує окремого запущеного сервера чи служби — уся БД зберігається в одному локальному файлі `.db`.

## Підсумок та підводні камені

- Виклик `db.Users.Add(...)` лише переводить сутність у стан `Added` в пам'яті; реальний `INSERT` у БД виконується тільки після виклику `db.SaveChanges()`.
- У реальних застосунках виклик `Database.EnsureCreated()` у конструкторі контексту варто використовувати обережно (він створює БД при першому запуску, але не сумісний із міграціями).
- Завжди звільняйте ресурси контексту через `using` або DI-контейнер.

# 7.1. Провайдер MS SQL Server (та адаптація під SQLite)

- **Глава:** 02. Провайдери баз даних
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/7.1.php](https://metanit.com/sharp/efcore/7.1.php)
- **Код прикладу:** [Lesson_7_1_SqlServerProvider.cs](../../src/02_DatabaseProviders/Lesson_7_1_SqlServerProvider.cs)

## Мета уроку

Навчитися підключати Entity Framework Core до **MS SQL Server** за допомогою офіційного провайдера `Microsoft.EntityFrameworkCore.SqlServer`, розуміти структуру рядка підключення (`Server`, `Database`, `Trusted_Connection`) та побачити, як перемикання провайдера через `DbContextOptions` дозволяє виконати той самий бізнес-код у локальному середовищі на **SQLite**.

## Теоретичний матеріал

### 1. Встановлення провайдера

Для роботи з MS SQL Server у проєкт необхідно додати NuGet-пакет:

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Цей пакет надає метод розширення `UseSqlServer(...)` для `DbContextOptionsBuilder` та транслює LINQ-запити у діалект **T-SQL**.

### 2. Параметри рядка підключення (Connection String)

У типовій конфігурації з Metanit рядок підключення виглядає так:

```text
Server=(localdb)\mssqllocaldb;Database=helloappdb;Trusted_Connection=True;
```

| Параметр | Значення у прикладі | Призначення |
| :--- | :--- | :--- |
| `Server` | `(localdb)\mssqllocaldb` | Назва сервера або екземпляра. `(localdb)\mssqllocaldb` — вбудований движок для розробки; для Express-версії зазвичай використовують `.\SQLEXPRESS`. |
| `Database` | `helloappdb` | Ім'я бази даних на сервері. |
| `Trusted_Connection` | `True` | Використання вбудованої автентифікації Windows замість логіна та пароля SQL. |
| `TrustServerCertificate` | `True` *(за потреби)* | У сучасних версіях `Microsoft.Data.SqlClient` шифрування увімкнене за замовчуванням; для локального сервера може знадобитися довіряти самопідписаному сертифікату. |

### 3. Перевірка активного провайдера та генерації SQL

У EF Core конкретний провайдер визначається на рівні `DbContextOptions`:
- Властивість `db.Database.ProviderName` повертає повну назву збірки активного провайдера (наприклад, `Microsoft.EntityFrameworkCore.SqlServer` або `Microsoft.EntityFrameworkCore.Sqlite`).
- Метод `query.ToQueryString()` дозволяє побачити точний SQL-запит, сформований провайдером, ще до його виконання в базі даних.

## Ключові фрагменти коду

### Конфігурація для MS SQL Server (як на Metanit)

```csharp
using Microsoft.EntityFrameworkCore;

public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            @"Server=(localdb)\mssqllocaldb;Database=helloappdb;Trusted_Connection=True;");
    }
}
```

### Адаптація через зовнішні `DbContextOptions` у цьому проєкті

```csharp
var connectionString = SqliteDbHelper.GetConnectionString("ch02_lesson_7_1");
var options = new DbContextOptionsBuilder<ApplicationContext>()
    .UseSqlite(connectionString)
    .Options;

using var db = new ApplicationContext(options);
Console.WriteLine(db.Database.ProviderName); // Microsoft.EntityFrameworkCore.Sqlite
```

## Особливості роботи в SQLite

Оскільки цей навчальний репозиторій працює без зовнішнього сервера MS SQL Server, приклад виконується на **SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`). Основні відмінності між T-SQL (MS SQL Server) та SQLite:

| Аспект | MS SQL Server (`UseSqlServer`) | SQLite (`UseSqlite`) |
| :--- | :--- | :--- |
| Екранування імен | Квадратні дужки `[Users]`, `[Name]` | Подвійні лапки `"Users"`, `"Name"` |
| Автоінкремент PK | `IDENTITY(1, 1)` | `INTEGER PRIMARY KEY AUTOINCREMENT` |
| Обмеження вибірки | `SELECT TOP(@p) ...` або `OFFSET ... FETCH` | `LIMIT @p` |
| Типи даних | Багатий набір (`nvarchar(max)`, `datetime2`, `decimal(18,2)`) | Динамічна типізація (`TEXT`, `INTEGER`, `REAL`, `BLOB`) |
| Зовнішній сервер | Потрібен LocalDB / SQL Server / Docker | Файлова БД у папці `Databases/` без сервера |

## Підсумок та підводні камені

- **Відмова від жорсткої прив'язки в `OnConfiguring`**: передача `DbContextOptions<TContext>` через конструктор дозволяє легко підміняти провайдер (наприклад, SQL Server у продакшені на SQLite у тестах чи навчальних проєктах).
- **`TrustServerCertificate=True`**: при переході на новіші версії драйвера `Microsoft.Data.SqlClient` (починаючи з EF Core 7+) локальні підключення без валідного TLS-сертифіката можуть падати з помилкою SSL, якщо не додати цей параметр або `Encrypt=False`.
- **Локалізація `LocalDB`**: `(localdb)\mssqllocaldb` працює лише на Windows за наявності встановленого SQL Server Express LocalDB; на Linux/macOS для розробки використовують Docker-контейнер із SQL Server або SQLite.

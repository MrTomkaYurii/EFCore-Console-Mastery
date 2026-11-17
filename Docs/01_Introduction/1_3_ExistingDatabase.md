# 1.3. Підключення до існуючої бази даних

- **Глава:** 01. Вступ до Entity Framework Core
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/1.3.php](https://metanit.com/sharp/efcore/1.3.php)
- **Код прикладу:** [`Lesson_1_3_ExistingDatabase.cs`](../../src/01_Introduction/Lesson_1_3_ExistingDatabase.cs)

## Мета уроку

Навчитися підключати EF Core до вже існуючої бази даних (підхід *Database First*), розуміти процес реверс-інжинірингу (*Scaffolding*) та команди генерації моделей і контексту за готовою схемою БД.

## Теоретичний матеріал

### Підхід Database First та Reverse Engineering
Коли база даних уже існує до початку розробки застосунку, є два шляхи:
1. Написати класи моделей та `DbContext` вручну, точно повторивши імена таблиць, стовпців і типів.
2. Скористатися **Reverse Engineering (Scaffolding)** — автоматичною генерацією C#-класів моделей і `DbContext` на основі метаданих існуючої БД.

Для реверс-інжинірингу в проєкті має бути встановлений пакет провайдера (наприклад, `Microsoft.EntityFrameworkCore.Sqlite`) та `Microsoft.EntityFrameworkCore.Design` (або `Microsoft.EntityFrameworkCore.Tools` для Visual Studio).

### Команди Scaffolding

| Середовище | Команда |
| --- | --- |
| **Visual Studio (PMC)** | `Scaffold-DbContext "Data Source=D:\helloapp.db" Microsoft.EntityFrameworkCore.Sqlite` |
| **.NET CLI (`dotnet-ef`)** | `dotnet ef dbcontext scaffold "Data Source=D:\helloapp.db" Microsoft.EntityFrameworkCore.Sqlite` |

Для роботи через .NET CLI попередньо встановлюється глобальний інструмент:
```bash
dotnet tool install --global dotnet-ef
```

### Що генерує скафолдер
- **`partial class` для кожної таблиці** (наприклад, `User`), щоб ви могли розширювати модель в окремому файлі без втрати змін при повторному скафолдингу.
- **`partial class <DbName>Context : DbContext`** з властивостями `DbSet<T>`, методом `OnConfiguring`, `OnModelCreating` (із Fluent API конфігурацією обмежень, індексів і зовнішніх ключів) та `OnModelCreatingPartial`.

## Ключові фрагменти коду

У прикладі до уроку ми спочатку створюємо БД SQLite через сирий `SqliteConnection` (імітуючи зовнішню БД), а потім підключаємося до неї через `ExistingDbContext` **без** виклику `Database.EnsureCreated()`:

```csharp
using (var rawConnection = new SqliteConnection(connectionString))
{
    rawConnection.Open();
    using var createCmd = rawConnection.CreateCommand();
    createCmd.CommandText = """
        CREATE TABLE Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NULL,
            Age INTEGER NOT NULL
        );
        INSERT INTO Users (Name, Age) VALUES ('Tom', 33);
        """;
    createCmd.ExecuteNonQuery();
}

using (var db = new ExistingDbContext(connectionString))
{
    var existingUsers = db.Users.ToList();
}
```

## Особливості роботи в SQLite

- У SQLite цілочисельний тип `INTEGER` є 64-бітним, тому `Scaffold-DbContext` для стовпців `INTEGER` у SQLite генерує властивості типу `long` (а не `int`, як у прикладах із SQL Server `INT`).
- Після скафолдингу рядок підключення потрапляє прямо в `OnConfiguring` із попередженням `#warning To protect potentially sensitive information in your connection string...` — у реальних проєктах його слід перенести в конфігурацію (`appsettings.json` або секрети користувача).

## Підсумок та підводні камені

- При роботі з існуючою БД **не викликайте** `Database.EnsureCreated()` чи `Database.EnsureDeleted()` у конструкторі контексту — покладайтеся на існуючу схему.
- Повторний запуск `Scaffold-DbContext` із прапорцем `-Force` / `--force` перезапише згенеровані файли, тому власні доповнення до моделей тримайте в окремих `partial`-файлах.
- Якщо схема БД змінюється зовнішньою командою DBA, скафолдинг можна запускати повторно, оновлюючи лише потрібні таблиці параметром `--table`.

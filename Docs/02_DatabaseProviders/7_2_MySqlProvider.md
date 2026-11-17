# 7.2. Провайдер MySQL (та адаптація під SQLite)

- **Глава:** 02. Провайдери баз даних
- **Оригінал на Metanit:** [https://metanit.com/sharp/efcore/7.2.php](https://metanit.com/sharp/efcore/7.2.php)
- **Код прикладу:** [Lesson_7_2_MySqlProvider.cs](../../src/02_DatabaseProviders/Lesson_7_2_MySqlProvider.cs)

## Мета уроку

Ознайомитися з підключенням Entity Framework Core до СКБД **MySQL** (та сумісної **MariaDB**), порівняти доступні NuGet-провайдери (`Pomelo.EntityFrameworkCore.MySql` та `MySql.EntityFrameworkCore`), зрозуміти роль об'єкта `MySqlServerVersion` і перевірити поведінку `DbContextOptions` та `ToQueryString()` на прикладі SQLite.

## Теоретичний матеріал

### 1. Вибір NuGet-пакета для MySQL

Для роботи з MySQL в EF Core існують два основні провайдери:

| Провайдер | Розробник | Особливості |
| :--- | :--- | :--- |
| `Pomelo.EntityFrameworkCore.MySql` | Спільнота (Pomelo Foundation) | **Рекомендований.** Висока швидкість, підтримка MySQL і MariaDB, оперативне оновлення під нові версії EF Core. |
| `MySql.EntityFrameworkCore` | Oracle (офіційний) | Офіційний пакет, але історично повільніше отримує підтримку нових версій EF Core та окремих функцій. |

Встановлення рекомендованого пакета:

```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

### 2. Параметри підключення та версія сервера

У `Pomelo.EntityFrameworkCore.MySql` метод `UseMySql(...)` приймає два обов'язкові параметри:
1. **Рядок підключення**: `server=localhost;user=root;password=123456789;database=usersdb;`
2. **Версія сервера (`ServerVersion`)**: наприклад, `new MySqlServerVersion(new Version(8, 0, 25))`.

Навіщо вказувати версію? Генерація SQL залежить від можливостей конкретної версії MySQL (наприклад, підтримка CTE, віконних функцій, `JSON`-операторів чи `CHECK`-обмежень). Можна також використати `ServerVersion.AutoDetect(connectionString)`, але тоді під час старту виконуватиметься додатковий запит до сервера для визначення версії.

## Ключові фрагменти коду

### Конфігурація для MySQL (як на Metanit)

```csharp
using Microsoft.EntityFrameworkCore;

public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(
            "server=localhost;user=root;password=123456789;database=usersdb;",
            new MySqlServerVersion(new Version(8, 0, 25)));
    }
}
```

### Робота з `DbContextOptions` у цьому навчальному проєкті (SQLite)

```csharp
var connectionString = SqliteDbHelper.GetConnectionString("ch02_lesson_7_2");
var options = new DbContextOptionsBuilder<ApplicationContext>()
    .UseSqlite(connectionString)
    .Options;

using var db = new ApplicationContext(options);
var query = db.Users
    .Where(u => u.Age >= 20 && u.Name != null)
    .OrderByDescending(u => u.Age)
    .Take(5);

Console.WriteLine(query.ToQueryString());
```

## Особливості роботи в SQLite

У цьому проєкті замість запущеного сервера MySQL використовується **SQLite**. Основні відмінності:

| Аспект | MySQL (`Pomelo.EntityFrameworkCore.MySql`) | SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) |
| :--- | :--- | :--- |
| Екранування імен | Зворотні лапки `` `Users`.`Name` `` | Подвійні лапки `"Users"."Name"` |
| Автоінкремент PK | `AUTO_INCREMENT` | `AUTOINCREMENT` |
| Чутливість рядків до регістру | За замовчуванням нечутлива (`utf8mb4_0900_ai_ci`) | Чутлива до регістру для ASCII (`BINARY`) |
| Вимога версії сервера | Потрібен `MySqlServerVersion` / `MariaDbServerVersion` | Не потрібна (версія движка вбудована в пакет) |

## Підсумок та підводні камені

- **Сумісність версій пакетів**: `Pomelo.EntityFrameworkCore.MySql` має суворо відповідати мажорній версії `Microsoft.EntityFrameworkCore` (наприклад, 8.x до EF Core 8, 9.x до EF Core 9).
- **Уникайте `ServerVersion.AutoDetect()` у DI при високому навантаженні або офлайн-генерації міграцій**: для `dotnet ef migrations add` без підключення до БД краще явно задавати `new MySqlServerVersion(...)`.
- **Кодування рядків**: для повноцінної підтримки емодзі та кирилиці в MySQL переконайтеся, що база має кодування `utf8mb4`.

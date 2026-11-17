using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 2.2. Управління схемою БД та міграції.
/// Демонстрація програмного інспектування міграцій (GetMigrations, GetAppliedMigrations,
/// GetPendingMigrations) та генерації SQL-скрипта створення схеми через GenerateCreateScript().
/// </summary>
public class Lesson_2_2_Migrations : ILesson
{
    public string Id => "2.2";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Управління схемою БД та міграції";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.2.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 8;

    /// <summary>
    /// Запускає демонстрацію програмної інспекції міграцій (GetMigrations, GetAppliedMigrations,
    /// GetPendingMigrations) та генерації DDL SQL-скрипта через GenerateCreateScript().
    /// </summary>
    public void Run()
    {
        var connectionString = SqliteDbHelper.GetConnectionString("lesson_2_2");

        using var db = new L2_2.MigrationDemoContext(connectionString);
        SqliteDbHelper.RecreateDatabase(db);

        // 1. Перевірка стану міграцій через DatabaseFacade
        // У реальному проєкті міграції створюються через CLI: dotnet ef migrations add InitialCreate
        // і застосовуються через dotnet ef database update або програмно через db.Database.Migrate().
        SqliteDbHelper.PrintSection("1. Інспекція міграцій у поточному контексті");
        // GetMigrations() шукає у збірці класи, успадковані від Migration та позначені [DbContext(typeof(MigrationDemoContext))]
        var allMigrations = db.Database.GetMigrations().ToList();
        // GetAppliedMigrations() читає таблицю __EFMigrationsHistory у БД (якщо її немає — повертає порожній список)
        var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
        // GetPendingMigrations() = allMigrations мінус appliedMigrations (те, що виконає db.Database.Migrate())
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();

        Console.WriteLine($"Усього визначено класів міграцій у збірці для цього контексту: {allMigrations.Count}");
        Console.WriteLine($"Застосованих міграцій (__EFMigrationsHistory):               {appliedMigrations.Count}");
        Console.WriteLine($"Очікуючих міграцій (PendingMigrations):                      {pendingMigrations.Count}");
        Console.WriteLine("Примітка: у навчальному консольному проєкті з багатьма контекстами БД створюється через EnsureCreated(),");
        Console.WriteLine("тому таблиця __EFMigrationsHistory відсутня, а списки міграцій порожні.");

        // 2. Генерація DDL SQL-скрипта для поточної моделі
        // GenerateCreateScript() повертає точний SQL (CREATE TABLE, CREATE INDEX тощо),
        // який EF Core виконує всередині EnsureCreated() для обраного провайдера (тут — діалект SQLite).
        // Зручно для рев'ю схеми БД або передачі DBA без запуску міграцій.
        SqliteDbHelper.PrintSection("2. Генерація SQL-скрипта схеми (GenerateCreateScript)");
        string createScript = db.Database.GenerateCreateScript();
        Console.WriteLine(createScript.Trim());

        // 3. Перевірка роботи з даними за згенерованою схемою
        SqliteDbHelper.PrintSection("3. Робота з моделлю після створення схеми");
        db.Users.Add(new L2_2.User { Name = "Tom", Age = 33, Position = "Developer" });
        db.SaveChanges();

        foreach (var u in db.Users.ToList())
        {
            Console.WriteLine($"  {u.Id}. {u.Name} ({u.Age}) — {u.Position}");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch01.L2_2
{
    /// <summary>
    /// Модель користувача з додатковою властивістю Position (ілюструє еволюцію схеми,
    /// для якої в реальному проєкті створюють нову міграцію, наприклад AddUserPosition).
    /// Примітка: у SQLite ALTER TABLE має обмеження (наприклад, історично не підтримувалося видалення/зміна
    /// окремих обмежень), тому EF Core під час міграцій інколи виконує перестворення таблиці (table rebuild),
    /// тоді як у MS SQL Server достатньо простого ALTER TABLE.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Первинний ключ таблиці Users (INTEGER PRIMARY KEY AUTOINCREMENT у SQLite).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача (TEXT NULL у SQLite).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Вік користувача (INTEGER NOT NULL у SQLite).
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Посада користувача (TEXT NULL у SQLite).
        /// </summary>
        public string? Position { get; set; }
    }

    /// <summary>
    /// Контекст даних для демонстрації інспекції міграцій та генерації DDL-скрипта схеми.
    /// </summary>
    public class MigrationDemoContext(string connectionString) : DbContext
    {
        /// <summary>
        /// Набір користувачів, що відповідає таблиці "Users".
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Налаштовує провайдер SQLite, що визначає діалект SQL для GenerateCreateScript() та міграцій.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}

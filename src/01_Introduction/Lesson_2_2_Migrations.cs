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

    public void Run()
    {
        var connectionString = SqliteDbHelper.GetConnectionString("lesson_2_2");

        using var db = new L2_2.MigrationDemoContext(connectionString);
        SqliteDbHelper.RecreateDatabase(db);

        // 1. Перевірка стану міграцій через DatabaseFacade
        SqliteDbHelper.PrintSection("1. Інспекція міграцій у поточному контексті");
        var allMigrations = db.Database.GetMigrations().ToList();
        var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();

        Console.WriteLine($"Усього визначено класів міграцій у збірці для цього контексту: {allMigrations.Count}");
        Console.WriteLine($"Застосованих міграцій (__EFMigrationsHistory):               {appliedMigrations.Count}");
        Console.WriteLine($"Очікуючих міграцій (PendingMigrations):                      {pendingMigrations.Count}");
        Console.WriteLine("Примітка: у навчальному консольному проєкті з багатьма контекстами БД створюється через EnsureCreated(),");
        Console.WriteLine("тому таблиця __EFMigrationsHistory відсутня, а списки міграцій порожні.");

        // 2. Генерація DDL SQL-скрипта для поточної моделі
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
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Position { get; set; }
    }

    public class MigrationDemoContext(string connectionString) : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}

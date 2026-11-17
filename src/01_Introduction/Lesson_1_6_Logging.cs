using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 1.6. Логування операцій.
/// Демонстрація LogTo, фільтрації за LogLevel, RelationalEventId та DbLoggerCategory.
/// </summary>
public class Lesson_1_6_Logging : ILesson
{
    public string Id => "1.6";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Логування операцій";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.6.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 7;

    public void Run()
    {
        var connectionString = SqliteDbHelper.GetConnectionString("lesson_1_6");

        // Спочатку створимо чисту БД без логування DDL
        using (var initDb = new L1_6.PlainContext(connectionString))
        {
            SqliteDbHelper.RecreateDatabase(initDb);
        }

        // 1. Фільтрація за рівнем LogLevel (LogLevel.Information)
        SqliteDbHelper.PrintSection("1. LogTo з мінімальним рівнем LogLevel.Information");
        var infoOptions = new DbContextOptionsBuilder<L1_6.LoggedContext>()
            .UseSqlite(connectionString)
            .LogTo(msg => Console.WriteLine($"  [LOG] {msg}"), LogLevel.Information)
            .Options;

        using (var db = new L1_6.LoggedContext(infoOptions))
        {
            db.Users.Add(new L1_6.User { Name = "Tom", Age = 33 });
            db.SaveChanges();
        }

        // 2. Фільтрація за ідентифікатором події RelationalEventId.CommandExecuted
        SqliteDbHelper.PrintSection("2. Фільтрація за подією RelationalEventId.CommandExecuted");
        var eventOptions = new DbContextOptionsBuilder<L1_6.LoggedContext>()
            .UseSqlite(connectionString)
            .LogTo(msg => Console.WriteLine($"  [SQL-EVENT] {msg}"), new[] { RelationalEventId.CommandExecuted })
            .Options;

        using (var db = new L1_6.LoggedContext(eventOptions))
        {
            var users = db.Users.Where(u => u.Age >= 25).ToList();
            Console.WriteLine($"Отримано користувачів: {users.Count}");
        }

        // 3. Фільтрація за категорією DbLoggerCategory.Database.Command
        SqliteDbHelper.PrintSection("3. Фільтрація за категорією DbLoggerCategory.Database.Command");
        var categoryOptions = new DbContextOptionsBuilder<L1_6.LoggedContext>()
            .UseSqlite(connectionString)
            .LogTo(
                msg => Console.WriteLine($"  [CATEGORY] {msg}"),
                new[] { DbLoggerCategory.Database.Command.Name },
                LogLevel.Information)
            .Options;

        using (var db = new L1_6.LoggedContext(categoryOptions))
        {
            db.Users.Add(new L1_6.User { Name = "Alice", Age = 26 });
            db.SaveChanges();

            foreach (var u in db.Users.ToList())
            {
                Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch01.L1_6
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class PlainContext(string connectionString) : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString);
        }
    }

    public class LoggedContext(DbContextOptions<LoggedContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
    }
}

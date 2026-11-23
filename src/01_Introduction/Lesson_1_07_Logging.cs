using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 1.7. Логування операцій.
/// Демонстрація LogTo, фільтрації за LogLevel, RelationalEventId та DbLoggerCategory.
/// </summary>
public class Lesson_1_07_Logging : ILesson
{
    public string Id => "1.7";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Логування операцій";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.6.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 7;

    /// <summary>
    /// Запускає демонстрацію вбудованого механізму логування EF Core (LogTo):
    /// 1) фільтрація за мінімальним рівнем LogLevel;
    /// 2) фільтрація за конкретним ідентифікатором події RelationalEventId;
    /// 3) фільтрація за категорією DbLoggerCategory.
    /// </summary>
    public void Run()
    {
        var connectionString = SqlServerDbHelper.ConnectionString;

        // Спочатку створимо чисту БД без логування DDL (CREATE TABLE),
        // щоб не засмічувати вивід першого прикладу
        using (var initDb = new L1_6.PlainContext(connectionString))
        {
            SqlServerDbHelper.RecreateSchema(initDb, "l1_07");
        }

        // 1. Фільтрація за рівнем LogLevel (LogLevel.Information)
        // LogTo приймає делегат Action<string> (тут — вивід у консоль; у реальних проєктах — у файл або ILogger).
        // Рівень Information показує виконані SQL-команди, час виконання та ініціалізацію контексту.
        // Примітка: значення параметрів SQL (@p0, @p1) за замовчуванням приховані (потрібен EnableSensitiveDataLogging() лише для dev!).
        SqlServerDbHelper.PrintSection("1. LogTo з мінімальним рівнем LogLevel.Information");
        var infoOptions = new DbContextOptionsBuilder<L1_6.LoggedContext>()
            .UseSqlServer(connectionString)
            .LogTo(msg => Console.WriteLine($"  [LOG] {msg}"), LogLevel.Information)
            .Options;

        using (var db = new L1_6.LoggedContext(infoOptions))
        {
            db.Users.Add(new L1_6.User { Name = "Tom", Age = 33 });
            // У логах буде видно відкриття з'єднання, транзакцію та INSERT INTO "Users" ... RETURNING "Id"
            db.SaveChanges();
        }

        // 2. Фільтрація за ідентифікатором події RelationalEventId.CommandExecuted
        // Це найзручніший спосіб бачити ТІЛЬКИ виконані SQL-запити та час їхнього виконання,
        // відкидаючи службові повідомлення про ініціалізацію моделі, з'єднання та транзакції.
        SqlServerDbHelper.PrintSection("2. Фільтрація за подією RelationalEventId.CommandExecuted");
        var eventOptions = new DbContextOptionsBuilder<L1_6.LoggedContext>()
            .UseSqlServer(connectionString)
            .LogTo(msg => Console.WriteLine($"  [SQL-EVENT] {msg}"), new[] { RelationalEventId.CommandExecuted })
            .Options;

        using (var db = new L1_6.LoggedContext(eventOptions))
        {
            // Побачимо точний SQL: SELECT "u"."Id", "u"."Age", "u"."Name" FROM "Users" AS "u" WHERE "u"."Age" >= 25
            var users = db.Users.Where(u => u.Age >= 25).ToList();
            Console.WriteLine($"Отримано користувачів: {users.Count}");
        }

        // 3. Фільтрація за категорією DbLoggerCategory.Database.Command
        // Категорія Database.Command охоплює події виконання команд (CommandExecuting, CommandExecuted, CommandError),
        // але відфільтровує категорії на кшталт ChangeTracking, Query або Infrastructure.
        SqlServerDbHelper.PrintSection("3. Фільтрація за категорією DbLoggerCategory.Database.Command");
        var categoryOptions = new DbContextOptionsBuilder<L1_6.LoggedContext>()
            .UseSqlServer(connectionString)
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
    /// <summary>
    /// Сутність користувача для демонстрації логування SQL-команд.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Первинний ключ таблиці Users.
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
    }

    /// <summary>
    /// Службовий контекст без логування — використовується для підготовки чистої БД.
    /// </summary>
    public class PlainContext(string connectionString) : DbContext
    {
        /// <summary>
        /// Таблиця користувачів.
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Налаштовує провайдер SQLite без логування.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    /// <summary>
    /// Контекст, який приймає зовнішні параметри DbContextOptions (включно з налаштуваннями LogTo).
    /// </summary>
    public class LoggedContext(DbContextOptions<LoggedContext> options) : DbContext(options)
    {
        /// <summary>
        /// Таблиця користувачів.
        /// </summary>
        public DbSet<User> Users => Set<User>();
    }
}

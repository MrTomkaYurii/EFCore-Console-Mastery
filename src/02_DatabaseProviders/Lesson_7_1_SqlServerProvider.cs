using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch02;

/// <summary>
/// Урок 7.1: Провайдер MS SQL Server (та адаптація під SQLite).
/// Оригінал: https://metanit.com/sharp/efcore/7.1.php
/// </summary>
public class Lesson_7_1_SqlServerProvider : ILesson
{
    public string Id => "7.1";
    public string Chapter => "02. Провайдери баз даних";
    public string Title => "Провайдер MS SQL Server (та адаптація під SQLite)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/7.1.php";
    public int ChapterOrder => 2;
    public int LessonOrder => 1;

    // Ізольовані моделі та контекст для цього уроку, щоб уникнути конфліктів імен у проєкті.
    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст даних, що приймає зовнішні DbContextOptions.
    /// У реальному проєкті з MS SQL Server сюди передається UseSqlServer(...),
    /// а в цьому навчальному середовищі — UseSqlite(...).
    /// </summary>
    private sealed class ApplicationContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        /*
         * Варіант із Metanit для MS SQL Server (потрібен NuGet-пакет Microsoft.EntityFrameworkCore.SqlServer):
         *
         * protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         * {
         *     optionsBuilder.UseSqlServer(
         *         @"Server=(localdb)\mssqllocaldb;Database=helloappdb;Trusted_Connection=True;");
         * }
         *
         * Пояснення параметрів рядка підключення MS SQL Server:
         * - Server=(localdb)\mssqllocaldb  — локальний движок SQL Server LocalDB для розробки
         *   (для SQL Server Express зазвичай використовується .\SQLEXPRESS).
         * - Database=helloappdb            — назва бази даних.
         * - Trusted_Connection=True        — автентифікація Windows.
         * - TrustServerCertificate=True    — часто потрібна в сучасних версіях Microsoft.Data.SqlClient.
         */
    }

    public void Run()
    {
        SqliteDbHelper.PrintSection("1. Налаштування DbContextOptions та перевірка провайдера");

        // Конфігуруємо DbContextOptions через DbContextOptionsBuilder для SQLite
        var connectionString = SqliteDbHelper.GetConnectionString("ch02_lesson_7_1");
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(connectionString)
            .Options;

        // Створюємо (перестворюємо) базу даних та додаємо тестові дані (як у прикладі Metanit)
        using (var db = new ApplicationContext(options))
        {
            SqliteDbHelper.RecreateDatabase(db);

            Console.WriteLine($"Активний провайдер EF Core: {db.Database.ProviderName}");
            Console.WriteLine("Еквівалент для MS SQL Server: Microsoft.EntityFrameworkCore.SqlServer");
            Console.WriteLine("NuGet-пакет для MS SQL Server: Microsoft.EntityFrameworkCore.SqlServer");

            var user1 = new User { Name = "Tom", Age = 33 };
            var user2 = new User { Name = "Alice", Age = 26 };

            db.Users.AddRange(user1, user2);
            db.SaveChanges();
        }

        SqliteDbHelper.PrintSection("2. Отримання даних з БД");

        using (var db = new ApplicationContext(options))
        {
            var users = db.Users.ToList();
            Console.WriteLine("Users list:");
            foreach (var u in users)
            {
                Console.WriteLine($"{u.Id}.{u.Name} - {u.Age}");
            }
        }

        SqliteDbHelper.PrintSection("3. Генерація SQL-запиту (ToQueryString)");

        using (var db = new ApplicationContext(options))
        {
            int minAge = 25;
            // Метод ToQueryString() показує точний SQL, який генерує поточний провайдер
            var query = db.Users
                .Where(u => u.Age > minAge)
                .OrderBy(u => u.Name)
                .Take(10);

            Console.WriteLine("Згенерований SQLite SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();
            Console.WriteLine("Примітка: у MS SQL Server замість подвійних лапок \"Users\" використовуються");
            Console.WriteLine("квадратні дужки [Users], а замість LIMIT @__p_1 — конструкція TOP(@__p_1).");
        }
    }
}

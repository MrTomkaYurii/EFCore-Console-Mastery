using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch02;

/// <summary>
/// Урок 7.3: Провайдер PostgreSQL (та адаптація під SQLite).
/// Оригінал: https://metanit.com/sharp/efcore/7.3.php
/// </summary>
public class Lesson_7_3_PostgreSqlProvider : ILesson
{
    public string Id => "7.3";
    public string Chapter => "02. Провайдери баз даних";
    public string Title => "Провайдер PostgreSQL (та адаптація під SQLite)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/7.3.php";
    public int ChapterOrder => 2;
    public int LessonOrder => 3;

    // Ізольована модель User для цього уроку
    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст даних, що приймає DbContextOptions.
    /// У продакшені з PostgreSQL використовується UseNpgsql(...),
    /// у цьому проєкті — UseSqlite(...).
    /// </summary>
    private sealed class ApplicationContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        /*
         * Варіант із Metanit для PostgreSQL (NuGet-пакет Npgsql.EntityFrameworkCore.PostgreSQL):
         *
         * protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         * {
         *     optionsBuilder.UseNpgsql(
         *         "Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=пароль_от_postgres");
         * }
         *
         * Міграції та Reverse Engineering (Metanit 7.3):
         * - Для міграцій потрібен пакет Microsoft.EntityFrameworkCore.Tools:
         *     Add-Migration Initial
         *     Update-Database
         * - Для генерації моделей із наявної БД PostgreSQL:
         *     Scaffold-DbContext "Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=..." Npgsql.EntityFrameworkCore.PostgreSQL
         */
    }

    public void Run()
    {
        SqliteDbHelper.PrintSection("1. Конфігурація DbContextOptions (SQLite замість PostgreSQL)");

        var connectionString = SqliteDbHelper.GetConnectionString("ch02_lesson_7_3");
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(connectionString)
            .Options;

        using (var db = new ApplicationContext(options))
        {
            SqliteDbHelper.RecreateDatabase(db);

            Console.WriteLine($"Активний провайдер EF Core: {db.Database.ProviderName}");
            Console.WriteLine("Цільовий провайдер з Metanit: Npgsql.EntityFrameworkCore.PostgreSQL");
            Console.WriteLine("Метод підключення: optionsBuilder.UseNpgsql(\"Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=...\")");

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

        SqliteDbHelper.PrintSection("3. Генерація SQL-запиту (ToQueryString) та порівняння з PostgreSQL");

        using (var db = new ApplicationContext(options))
        {
            var query = db.Users
                .Where(u => u.Name != null && EF.Functions.Like(u.Name, "T%"))
                .OrderBy(u => u.Id);

            Console.WriteLine("Згенерований SQLite SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();
            Console.WriteLine("Примітка: як і SQLite, PostgreSQL використовує подвійні лапки \"Users\" для ідентифікаторів,");
            Console.WriteLine("але вони чутливі до регістру. Для пошуку без урахування регістру в Npgsql є EF.Functions.ILike().");
        }
    }
}

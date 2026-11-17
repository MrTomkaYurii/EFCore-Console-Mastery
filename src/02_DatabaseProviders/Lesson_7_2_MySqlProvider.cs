using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch02;

/// <summary>
/// Урок 7.2: Провайдер MySQL (та адаптація під SQLite).
/// Оригінал: https://metanit.com/sharp/efcore/7.2.php
/// </summary>
public class Lesson_7_2_MySqlProvider : ILesson
{
    public string Id => "7.2";
    public string Chapter => "02. Провайдери баз даних";
    public string Title => "Провайдер MySQL (та адаптація під SQLite)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/7.2.php";
    public int ChapterOrder => 2;
    public int LessonOrder => 2;

    // Ізольована модель User для цього уроку
    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст даних, налаштований через зовнішні DbContextOptions.
    /// У середовищі без сервера MySQL демонструється через SQLite.
    /// </summary>
    private sealed class ApplicationContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        /*
         * Варіант із Metanit для MySQL:
         * Рекомендований NuGet-пакет: Pomelo.EntityFrameworkCore.MySql
         * (Також існує офіційний пакет від Oracle: MySql.EntityFrameworkCore,
         * але Pomelo має кращу підтримку можливостей EF Core та оновлюється швидше.)
         *
         * protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         * {
         *     optionsBuilder.UseMySql(
         *         "server=localhost;user=root;password=123456789;database=usersdb;",
         *         new MySqlServerVersion(new Version(8, 0, 25)));
         * }
         *
         * Особливість Pomelo: другим параметром передається версія сервера MySQL
         * (або ServerVersion.AutoDetect(connectionString), що виконує додатковий запит при старті).
         */
    }

    public void Run()
    {
        SqliteDbHelper.PrintSection("1. Конфігурація DbContextOptions (SQLite замість MySQL)");

        var connectionString = SqliteDbHelper.GetConnectionString("ch02_lesson_7_2");
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(connectionString)
            .Options;

        using (var db = new ApplicationContext(options))
        {
            SqliteDbHelper.RecreateDatabase(db);

            Console.WriteLine($"Активний провайдер EF Core: {db.Database.ProviderName}");
            Console.WriteLine("Цільовий провайдер з Metanit: Pomelo.EntityFrameworkCore.MySql (або MySql.EntityFrameworkCore)");
            Console.WriteLine("Метод підключення: optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 25)))");

            var user1 = new User { Name = "Tom", Age = 33 };
            var user2 = new User { Name = "Alice", Age = 26 };

            db.Users.AddRange(user1, user2);
            db.SaveChanges();
            Console.WriteLine("Об'єкти успішно збережені.");
        }

        SqliteDbHelper.PrintSection("2. Читання даних з БД");

        using (var db = new ApplicationContext(options))
        {
            var users = db.Users.ToList();
            Console.WriteLine("Список об'єктів:");
            foreach (var u in users)
            {
                Console.WriteLine($"{u.Id}.{u.Name} - {u.Age}");
            }
        }

        SqliteDbHelper.PrintSection("3. Генерація SQL-запиту (ToQueryString)");

        using (var db = new ApplicationContext(options))
        {
            int minAge = 20;
            var query = db.Users
                .Where(u => u.Age >= minAge && u.Name != null)
                .OrderByDescending(u => u.Age)
                .Take(5);

            Console.WriteLine("Згенерований SQLite SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();
            Console.WriteLine("Примітка: у MySQL (Pomelo) ідентифікатори екрануються зворотними лапками `Users`.`Name`,");
            Console.WriteLine("а автоінкрементний ключ створюється з атрибутом AUTO_INCREMENT.");
        }
    }
}

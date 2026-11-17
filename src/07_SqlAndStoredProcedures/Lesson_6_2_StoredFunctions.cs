using EFCoreConsoleMastery.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_2
{
    /// <summary>Модель компанії для уроку 6.2.</summary>
    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Модель користувача для уроку 6.2.</summary>
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Контекст БД із демонстрацією проєкції функцій БД на C#-методи через HasDbFunction.
    /// У SQLite табличні функції (TVF) через CREATE FUNCTION не підтримуються,
    /// натомість можна реєструвати скалярні UDF через SqliteConnection.CreateFunction
    /// або проєктувати вбудовані функції SQLite (length, upper тощо).
    /// </summary>
    public class ApplicationContext : DbContext
    {
        private readonly SqliteConnection _connection;

        public ApplicationContext(SqliteConnection connection)
        {
            _connection = connection;
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Company> Companies => Set<Company>();

        /// <summary>
        /// Проєкція на користувацьку SQLite-функцію is_senior_user(age, threshold).
        /// Безпосередньо в пам'яті не викликається — транслюється EF Core у SQL.
        /// </summary>
        public static bool IsSeniorUser(int age, int threshold)
            => throw new NotSupportedException("Цей метод призначений лише для LINQ-запитів EF Core.");

        /// <summary>
        /// Проєкція на користувацьку SQLite-функцію format_user_badge(name, age).
        /// </summary>
        public static string FormatUserBadge(string name, int age)
            => throw new NotSupportedException("Цей метод призначений лише для LINQ-запитів EF Core.");

        /// <summary>
        /// Проєкція на вбудовану скалярну функцію SQLite length(text).
        /// </summary>
        public static int SqliteLength(string? text)
            => throw new NotSupportedException("Цей метод призначений лише для LINQ-запитів EF Core.");

        // -------------------------------------------------------------------------
        // ПРИМІТКА ДЛЯ MS SQL SERVER (Metanit 6.2):
        // У SQL Server таблична функція (CREATE FUNCTION [dbo].[GetUsersByAge](@age int)
        // RETURNS TABLE ...) проєктується на метод екземпляра DbContext так:
        //
        // public IQueryable<User> GetUsersByAge(int age)
        //     => FromExpression(() => GetUsersByAge(age));
        //
        // І реєструється в OnModelCreating:
        // modelBuilder.HasDbFunction(() => GetUsersByAge(default));
        // -------------------------------------------------------------------------

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Передаємо вже відкрите SqliteConnection із зареєстрованими UDF
            optionsBuilder.UseSqlite(_connection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Реєструємо скалярні функції в моделі EF Core
            modelBuilder.HasDbFunction(
                    typeof(ApplicationContext).GetMethod(nameof(IsSeniorUser), new[] { typeof(int), typeof(int) })!)
                .HasName("is_senior_user");

            modelBuilder.HasDbFunction(
                    typeof(ApplicationContext).GetMethod(nameof(FormatUserBadge), new[] { typeof(string), typeof(int) })!)
                .HasName("format_user_badge");

            modelBuilder.HasDbFunction(
                    typeof(ApplicationContext).GetMethod(nameof(SqliteLength), new[] { typeof(string) })!)
                .HasName("length");
        }
    }
}

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures
{
    using EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_2;

    /// <summary>
    /// Урок 6.2: Збережені функції в EF Core (адаптація для SQLite через UDF + HasDbFunction).
    /// </summary>
    public class Lesson_6_2_StoredFunctions : ILesson
    {
        public string Id => "6.2";
        public string Chapter => "07. SQL в Entity Framework Core";
        public string Title => "Збережені функції (SQLite UDF / HasDbFunction адаптація)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/6.2.php";
        public int ChapterOrder => 7;
        public int LessonOrder => 2;

        public void Run()
        {
            string dbName = "lesson_6_2_stored_functions";
            string dbPath = SqliteDbHelper.GetDbPath(dbName);
            string connectionString = SqliteDbHelper.GetConnectionString(dbName);

            // Очищаємо попередню БД до відкриття зовнішнього SqliteConnection (інакше файл буде заблоковано)
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            // Створюємо та відкриваємо SqliteConnection, щоб зареєструвати користувацькі функції (UDF)
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Реєструємо C#-делегати як SQLite-функції (аналог скалярних збережених функцій)
            connection.CreateFunction<int, int, bool>(
                "is_senior_user",
                (age, threshold) => age >= threshold,
                isDeterministic: true);

            connection.CreateFunction<string, int, string>(
                "format_user_badge",
                (name, age) => $"{name} [{age} р.]",
                isDeterministic: true);

            // 1. Ініціалізація бази даних
            using (var db = new ApplicationContext(connection))
            {
                db.Database.EnsureCreated();

                Company microsoft = new() { Name = "Microsoft" };
                Company google = new() { Name = "Google" };
                db.Companies.AddRange(microsoft, google);

                db.Users.AddRange(
                    new User { Name = "Tom", Age = 36, Company = microsoft },
                    new User { Name = "Bob", Age = 39, Company = google },
                    new User { Name = "Alice", Age = 28, Company = microsoft },
                    new User { Name = "Kate", Age = 25, Company = google },
                    new User { Name = "Tomas", Age = 22, Company = microsoft },
                    new User { Name = "Tomek", Age = 42, Company = google }
                );
                db.SaveChanges();
            }

            using (var db = new ApplicationContext(connection))
            {
                // 2. Виклик SQLite UDF через LINQ (HasDbFunction)
                SqliteDbHelper.PrintSection("1. Виклик SQLite UDF через HasDbFunction у LINQ");

                int threshold = 30;
                var seniorBadges = db.Users
                    .Where(u => ApplicationContext.IsSeniorUser(u.Age, threshold))
                    .OrderBy(u => u.Age)
                    .Select(u => new
                    {
                        Badge = ApplicationContext.FormatUserBadge(u.Name ?? "Unknown", u.Age),
                        NameLength = ApplicationContext.SqliteLength(u.Name)
                    })
                    .ToList();

                Console.WriteLine($"Користувачі віком >= {threshold} (обчислено через UDF у SQLite):");
                foreach (var item in seniorBadges)
                    Console.WriteLine($"  - {item.Badge} (довжина імені = {item.NameLength})");

                // 3. Виклик зареєстрованої функції безпосередньо в сирому SQL (FromSqlRaw)
                SqliteDbHelper.PrintSection("2. Виклик функції безпосередньо у FromSqlRaw");

                // Аналогічно до виклику збереженої функції в SQL-запиті на SQL Server
                var youngerUsers = db.Users
                    .FromSqlRaw("SELECT * FROM Users WHERE is_senior_user(Age, {0}) = 0 ORDER BY Age", threshold)
                    .ToList();

                Console.WriteLine($"Користувачі віком < {threshold} (FromSqlRaw + UDF):");
                foreach (var u in youngerUsers)
                    Console.WriteLine($"  - {u.Name} ({u.Age} р.)");

                // 4. Пояснення відмінностей від SQL Server
                SqliteDbHelper.PrintSection("3. Порівняння з MS SQL Server (CREATE FUNCTION)");
                Console.WriteLine("SQLite не підтримує CREATE FUNCTION у DDL (на відміну від T-SQL).");
                Console.WriteLine("Проте Microsoft.Data.Sqlite дозволяє реєструвати C#-функції через CreateFunction(),");
                Console.WriteLine("а EF Core HasDbFunction() однаково працює для обох підходів (UDF та вбудованих функцій).");
            }
        }
    }
}

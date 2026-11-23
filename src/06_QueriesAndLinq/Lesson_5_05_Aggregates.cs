using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_5;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.5: Агрегатні операції (Any, All, Count, Sum, Min, Max, Average).
/// Показує обчислення агрегатів на стороні SQLite без завантаження всіх рядків у пам'ять.
/// </summary>
public class Lesson_5_05_Aggregates : ILesson
{
    public string Id => "5.5";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Агрегатні операції (Any, All, Count, Sum, Min, Max, Average)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.5.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 6;

    /// <summary>
    /// Запускає демонстрацію агрегатних операцій LINQ to Entities
    /// (Any, All, Count, Min, Max, Average, Sum), що виконуються на стороні SQLite.
    /// </summary>
    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            Company microsoft = new() { Name = "Microsoft" };
            Company google = new() { Name = "Google" };
            db.Companies.AddRange(microsoft, google);

            db.Users.AddRange(
                new User { Name = "Tom", Age = 36, Company = microsoft },
                new User { Name = "Bob", Age = 39, Company = google },
                new User { Name = "Alice", Age = 28, Company = microsoft },
                new User { Name = "Kate", Age = 25, Company = google }
            );
            db.SaveChanges();
        }

        using (var db = new AppDbContext())
        {
            // 1. Any та All — миттєве виконання (повертають bool одразу без .ToList())
            SqliteDbHelper.PrintSection("1. Перевірка наявності та відповідності умові (Any / All)");
            // Any транслюється в SELECT EXISTS (SELECT 1 FROM "Users" ... WHERE ...) — зупиняється на першому збігу!
            // Це набагато швидше, ніж db.Users.Count(...) > 0, яке змушує БД рахувати всі рядки.
            bool hasGoogleUsers = db.Users.Any(u => u.Company!.Name == "Google");
            // All транслюється в заперечення EXISTS: NOT EXISTS (... WHERE NOT (умова))
            bool allMicrosoft = db.Users.All(u => u.Company!.Name == "Microsoft");

            Console.WriteLine($"Чи є працівники Google (Any): {hasGoogleUsers}");
            Console.WriteLine($"Чи всі працюють у Microsoft (All): {allMicrosoft}");

            // 2. Count — повертає int (або LongCount для long)
            SqliteDbHelper.PrintSection("2. Кількість елементів (Count)");
            // Генерує SELECT COUNT(*) FROM "Users" — у пам'ять C# передається лише одне число.
            int totalUsers = db.Users.Count();
            int tomUsers = db.Users.Count(u => u.Name!.Contains("Tom"));

            Console.WriteLine($"Усього користувачів: {totalUsers}");
            Console.WriteLine($"Ім'я містить 'Tom': {tomUsers}");

            // 3. Min, Max, Average
            SqliteDbHelper.PrintSection("3. Мінімум, максимум та середнє (Min, Max, Average)");
            // Важливий нюанс: якщо вибірка порожня, Min/Max/Average для non-nullable int/double викинуть InvalidOperationException,
            // оскільки SQL MIN/MAX/AVG повертають NULL для порожньої таблиці. Для безпечної обробки порожніх наборів
            // використовуйте приведення типу: db.Users.Min(u => (int?)u.Age).
            int minAge = db.Users.Min(u => u.Age);
            int maxAge = db.Users.Max(u => u.Age);
            double avgMicrosoftAge = db.Users
                .Where(u => u.Company!.Name == "Microsoft")
                .Average(u => u.Age);

            Console.WriteLine($"Мінімальний вік: {minAge}");
            Console.WriteLine($"Максимальний вік: {maxAge}");
            Console.WriteLine($"Середній вік у Microsoft: {avgMicrosoftAge:F1}");

            // 4. Sum
            SqliteDbHelper.PrintSection("4. Сума значень (Sum)");
            // На відміну від Min/Max/Average, SQL COALESCE(SUM(...), 0) у EF Core поверне 0, якщо рядків немає.
            // Примітка щодо SQLite: для decimal (який у SQLite зберігається як TEXT) агрегати Sum/Average/Min/Max
            // можуть мати обмеження або вимагати конвертації на клієнті, тоді як int/double працюють нативно в SQL.
            int sumAll = db.Users.Sum(u => u.Age);
            int sumMicrosoft = db.Users
                .Where(u => u.Company!.Name == "Microsoft")
                .Sum(u => u.Age);

            Console.WriteLine($"Сумарний вік усіх користувачів: {sumAll}");
            Console.WriteLine($"Сумарний вік працівників Microsoft: {sumMicrosoft}");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_5
{
    /// <summary>Компанія (головна сутність).</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Навігаційна колекція користувачів компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Користувач (залежна сутність).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }

        /// <summary>Зовнішній ключ до таблиці Companies.</summary>
        public int CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.5 (агрегатні функції).</summary>
    public class AppDbContext : DbContext
    {
        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Таблиця компаній.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Підключення до локальної БД SQLite для уроку 5.5.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_5"));
        }
    }
}

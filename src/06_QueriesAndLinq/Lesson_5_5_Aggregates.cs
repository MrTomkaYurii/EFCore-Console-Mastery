using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_5;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.5: Агрегатні операції (Any, All, Count, Sum, Min, Max, Average).
/// Показує обчислення агрегатів на стороні SQLite без завантаження всіх рядків у пам'ять.
/// </summary>
public class Lesson_5_5_Aggregates : ILesson
{
    public string Id => "5.5";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Агрегатні операції (Any, All, Count, Sum, Min, Max, Average)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.5.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 6;

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
            // 1. Any та All
            SqliteDbHelper.PrintSection("1. Перевірка наявності та відповідності умові (Any / All)");
            bool hasGoogleUsers = db.Users.Any(u => u.Company!.Name == "Google");
            bool allMicrosoft = db.Users.All(u => u.Company!.Name == "Microsoft");

            Console.WriteLine($"Чи є працівники Google (Any): {hasGoogleUsers}");
            Console.WriteLine($"Чи всі працюють у Microsoft (All): {allMicrosoft}");

            // 2. Count
            SqliteDbHelper.PrintSection("2. Кількість елементів (Count)");
            int totalUsers = db.Users.Count();
            int tomUsers = db.Users.Count(u => u.Name!.Contains("Tom"));

            Console.WriteLine($"Усього користувачів: {totalUsers}");
            Console.WriteLine($"Ім'я містить 'Tom': {tomUsers}");

            // 3. Min, Max, Average
            SqliteDbHelper.PrintSection("3. Мінімум, максимум та середнє (Min, Max, Average)");
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
    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
    }

    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_5"));
        }
    }
}

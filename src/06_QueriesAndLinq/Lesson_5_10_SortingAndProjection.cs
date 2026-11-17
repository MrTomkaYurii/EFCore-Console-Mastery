using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_10;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.10: Сортування та проекція з бази даних.
/// Демонструє Select (в анонімний тип і DTO-клас), OrderBy, OrderByDescending, ThenBy, ThenByDescending.
/// </summary>
public class Lesson_5_10_SortingAndProjection : ILesson
{
    public string Id => "5.10";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Сортування та проекція з бази даних";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.10.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 3;

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
                new User { Name = "Kate", Age = 28, Company = google }
            );
            db.SaveChanges();
        }

        // 1. Проекція в анонімний тип за допомогою Select
        SqliteDbHelper.PrintSection("1. Проекція в анонімний тип (Select)");
        using (var db = new AppDbContext())
        {
            // Select вибирає лише потрібні стовпці, без завантаження зайвих даних і без Include
            var query = db.Users.Select(u => new
            {
                u.Name,
                u.Age,
                Company = u.Company!.Name
            });

            Console.WriteLine("Згенерований SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();

            foreach (var u in query.ToList())
            {
                Console.WriteLine($"{u.Name} ({u.Age}) - {u.Company}");
            }
        }

        // 2. Проекція у власний DTO-клас UserModel
        SqliteDbHelper.PrintSection("2. Проекція у визначений тип (UserModel)");
        using (var db = new AppDbContext())
        {
            var users = db.Users
                .Select(u => new UserModel
                {
                    Name = u.Name,
                    Age = u.Age,
                    Company = u.Company!.Name
                })
                .ToList();

            foreach (UserModel user in users)
            {
                Console.WriteLine($"{user.Name} ({user.Age}) - {user.Company}");
            }
        }

        // 3. Сортування: OrderBy, OrderByDescending, ThenBy, ThenByDescending
        SqliteDbHelper.PrintSection("3. Сортування (OrderBy, OrderByDescending, ThenBy)");
        using (var db = new AppDbContext())
        {
            Console.WriteLine("За зростанням імені (OrderBy):");
            foreach (var u in db.Users.OrderBy(u => u.Name).ToList())
                Console.WriteLine($"  {u.Id}. {u.Name} ({u.Age})");

            Console.WriteLine("\nЗа спаданням імені (OrderByDescending):");
            foreach (var u in db.Users.OrderByDescending(u => u.Name).ToList())
                Console.WriteLine($"  {u.Id}. {u.Name} ({u.Age})");

            Console.WriteLine("\nБагаторівневе сортування: Age (зростання), потім Company.Name (спадання):");
            var sorted = db.Users
                .Include(u => u.Company)
                .OrderBy(u => u.Age)
                .ThenByDescending(u => u.Company!.Name)
                .ToList();

            foreach (var u in sorted)
                Console.WriteLine($"  {u.Name} ({u.Age}) - {u.Company?.Name}");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_10
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

    /// <summary>DTO-модель для проекції результатів запиту.</summary>
    public class UserModel
    {
        public string? Name { get; set; }
        public string? Company { get; set; }
        public int Age { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_10"));
        }
    }
}

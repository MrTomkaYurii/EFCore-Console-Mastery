using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_1;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.1: Вступ до LINQ to Entities.
/// Демонструє два способи побудови запитів (синтаксис запитів LINQ та методи розширення)
/// і їх трансляцію в SQL-запити до SQLite.
/// </summary>
public class Lesson_5_1_LinqIntro : ILesson
{
    public string Id => "5.1";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Вступ до LINQ to Entities";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.1.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 1;

    public void Run()
    {
        // 1. Створення та початкове заповнення бази даних
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            Company microsoft = new() { Name = "Microsoft" };
            Company google = new() { Name = "Google" };
            db.Companies.AddRange(microsoft, google);

            User tom = new() { Name = "Tom", Age = 36, Company = microsoft };
            User bob = new() { Name = "Bob", Age = 39, Company = google };
            User alice = new() { Name = "Alice", Age = 28, Company = microsoft };
            User kate = new() { Name = "Kate", Age = 25, Company = google };
            db.Users.AddRange(tom, bob, alice, kate);

            db.SaveChanges();
        }

        // 2. Спосіб 1: Оператори LINQ (Query Syntax)
        SqliteDbHelper.PrintSection("1. Запит через оператори LINQ (Query Syntax)");
        using (var db = new AppDbContext())
        {
            var query = from user in db.Users.Include(u => u.Company)
                        where user.CompanyId == 1
                        select user;

            // Переглянемо згенерований SQL (через ToQueryString())
            Console.WriteLine("Згенерований SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();

            var users = query.ToList();
            foreach (var user in users)
            {
                Console.WriteLine($"{user.Name} ({user.Age}) - {user.Company?.Name}");
            }
        }

        // 3. Спосіб 2: Методи розширення LINQ (Method Syntax)
        SqliteDbHelper.PrintSection("2. Запит через методи розширення (Method Syntax)");
        using (var db = new AppDbContext())
        {
            var query = db.Users
                .Include(u => u.Company)
                .Where(u => u.CompanyId == 1);

            // Обидва способи транслюються в ідентичний SQL-запит
            var users = query.ToList();
            foreach (var user in users)
            {
                Console.WriteLine($"{user.Name} ({user.Age}) - {user.Company?.Name}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_1
{
    /// <summary>Компанія (головна сутність у зв'язку один-до-багатьох).</summary>
    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Користувач (залежна сутність).</summary>
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.1.</summary>
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_1"));
        }
    }
}

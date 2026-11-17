using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_3;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.3: З'єднання та групування таблиць (Join, GroupBy).
/// Демонструє явне з'єднання двох і трьох таблиць (Join / join ... equals) та групування (GroupBy / group ... by).
/// </summary>
public class Lesson_5_3_JoinAndGroupBy : ILesson
{
    public string Id => "5.3";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "З'єднання та групування таблиць (Join, GroupBy)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.3.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 4;

    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            Country usa = new() { Name = "USA" };
            Country ukraine = new() { Name = "Ukraine" };
            db.Countries.AddRange(usa, ukraine);

            Company microsoft = new() { Name = "Microsoft", Country = usa };
            Company google = new() { Name = "Google", Country = usa };
            Company softserve = new() { Name = "SoftServe", Country = ukraine };
            db.Companies.AddRange(microsoft, google, softserve);

            db.Users.AddRange(
                new User { Name = "Tom", Age = 36, Company = microsoft },
                new User { Name = "Bob", Age = 39, Company = google },
                new User { Name = "Alice", Age = 28, Company = microsoft },
                new User { Name = "Kate", Age = 25, Company = google },
                new User { Name = "Olena", Age = 30, Company = softserve }
            );
            db.SaveChanges();
        }

        // 1. З'єднання двох таблиць методом Join
        SqliteDbHelper.PrintSection("1. З'єднання двох таблиць (Join)");
        using (var db = new AppDbContext())
        {
            var users = db.Users.Join(
                db.Companies,        // другий набір
                u => u.CompanyId,    // зовнішній ключ із першого набору
                c => c.Id,           // первинний ключ із другого набору
                (u, c) => new        // результуюча проекція
                {
                    u.Name,
                    Company = c.Name,
                    u.Age
                });

            Console.WriteLine("Згенерований SQL:");
            Console.WriteLine(users.ToQueryString());
            Console.WriteLine();

            foreach (var u in users.ToList())
                Console.WriteLine($"{u.Name} ({u.Company}) - {u.Age}");
        }

        // 2. З'єднання трьох таблиць через оператори LINQ (join ... equals)
        SqliteDbHelper.PrintSection("2. З'єднання трьох таблиць (Query Syntax)");
        using (var db = new AppDbContext())
        {
            var users = from user in db.Users
                        join company in db.Companies on user.CompanyId equals company.Id
                        join country in db.Countries on company.CountryId equals country.Id
                        select new
                        {
                            user.Name,
                            Company = company.Name,
                            Country = country.Name,
                            user.Age
                        };

            foreach (var u in users.ToList())
                Console.WriteLine($"{u.Name} ({u.Company} - {u.Country}) - {u.Age}");
        }

        // 3. Групування даних (GroupBy та group ... by ... into)
        SqliteDbHelper.PrintSection("3. Групування (GroupBy)");
        using (var db = new AppDbContext())
        {
            // Варіант із методом розширення GroupBy
            var groups = db.Users
                .GroupBy(u => u.Company!.Name)
                .Select(g => new
                {
                    CompanyName = g.Key,
                    Count = g.Count(),
                    AvgAge = g.Average(u => u.Age)
                });

            Console.WriteLine("Згенерований SQL:");
            Console.WriteLine(groups.ToQueryString());
            Console.WriteLine();

            foreach (var group in groups.ToList())
            {
                Console.WriteLine($"{group.CompanyName}: {group.Count} працівн., середній вік = {group.AvgAge:F1}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_3
{
    public class Country
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CountryId { get; set; }
        public Country? Country { get; set; }
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
        public DbSet<Country> Countries { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_3"));
        }
    }
}

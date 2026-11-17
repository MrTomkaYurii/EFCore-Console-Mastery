using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_3;

    /// <summary>
    /// Урок 3.3: Жадне завантаження пов'язаних даних (Eager Loading) через Include та ThenInclude.
    /// </summary>
    public class Lesson_3_3_EagerLoadingInclude : ILesson
    {
        public string Id => "3.3";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Завантаження пов'язаних даних. Метод Include";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.3.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 4;

        public void Run()
        {
            using (var db = new AppContextL3_3())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var ukraine = new Country { Name = "Ukraine" };
                var usa = new Country { Name = "USA" };
                db.Countries.AddRange(ukraine, usa);

                var kyivTech = new Company { Name = "KyivTech", Country = ukraine };
                var microsoft = new Company { Name = "Microsoft", Country = usa };
                db.Companies.AddRange(kyivTech, microsoft);

                var dev = new Position { Name = "Developer" };
                var manager = new Position { Name = "Manager" };
                db.Positions.AddRange(dev, manager);

                db.Users.AddRange(
                    new User { Name = "Taras", Company = kyivTech, Position = dev },
                    new User { Name = "Olena", Company = kyivTech, Position = manager },
                    new User { Name = "Tom", Company = microsoft, Position = dev }
                );
                db.SaveChanges();
            }

            SqliteDbHelper.PrintSection("1. Без Include навігаційні властивості дорівнюють null");
            using (var db = new AppContextL3_3())
            {
                var usersWithoutInclude = db.Users.ToList();
                foreach (var u in usersWithoutInclude)
                {
                    Console.WriteLine($"{u.Name} -> Company: {u.Company?.Name ?? "NULL (не завантажено)"}");
                }
            }

            SqliteDbHelper.PrintSection("2. Жадне завантаження через Include + ThenInclude");
            using (var db = new AppContextL3_3())
            {
                // Include завантажує Company та Position; ThenInclude підтягує Country для Company (LEFT JOIN)
                var users = db.Users
                    .Include(u => u.Company)
                        .ThenInclude(c => c!.Country)
                    .Include(u => u.Position)
                    .ToList();

                foreach (var u in users)
                {
                    Console.WriteLine($"{u.Name} | Посада: {u.Position?.Name} | Компанія: {u.Company?.Name} ({u.Company?.Country?.Name})");
                }
            }

            SqliteDbHelper.PrintSection("3. Зворотне завантаження колекцій (Company -> Users)");
            using (var db = new AppContextL3_3())
            {
                var companies = db.Companies
                    .Include(c => c.Users)
                    .ToList();

                foreach (var c in companies)
                {
                    Console.WriteLine($"Компанія {c.Name}: {string.Join(", ", c.Users.Select(u => u.Name))}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_3
{
    public class Country
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<Company> Companies { get; set; } = new();
    }

    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CountryId { get; set; }
        public Country? Country { get; set; }
        public List<User> Users { get; set; } = new();
    }

    public class Position
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
    }

    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
        public int? PositionId { get; set; }
        public Position? Position { get; set; }
    }

    public class AppContextL3_3 : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Country> Countries { get; set; } = null!;
        public DbSet<Position> Positions { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_3"));
        }
    }
}

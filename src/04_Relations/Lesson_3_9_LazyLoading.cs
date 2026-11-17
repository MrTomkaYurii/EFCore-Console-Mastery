using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_9;

    /// <summary>
    /// Урок 3.9: Ліниве завантаження (Lazy Loading) через Microsoft.EntityFrameworkCore.Proxies.
    /// </summary>
    public class Lesson_3_9_LazyLoading : ILesson
    {
        public string Id => "3.9";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Ліниве завантаження (Lazy loading)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.9.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 6;

        public void Run()
        {
            using (var db = new AppContextL3_9())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var microsoft = new Company { Name = "Microsoft" };
                var google = new Company { Name = "Google" };
                db.Companies.AddRange(microsoft, google);

                db.Users.AddRange(
                    new User { Name = "Tom", Company = microsoft },
                    new User { Name = "Alice", Company = microsoft },
                    new User { Name = "Bob", Company = google },
                    new User { Name = "Kate", Company = google }
                );
                db.SaveChanges();
            }

            SqliteDbHelper.PrintSection("1. Ліниве завантаження при зверненні до virtual-властивості Company");
            using (var db = new AppContextL3_9())
            {
                // Без Include! Запит витягує лише таблицю Users.
                var users = db.Users.ToList();
                foreach (var user in users)
                {
                    // Проксі-клас автоматично виконує додатковий SELECT до Companies, якщо компанії ще немає в контексті
                    Console.WriteLine($"{user.Name} -> {user.Company?.Name}");
                }
            }

            SqliteDbHelper.PrintSection("2. Ліниве завантаження колекції virtual Users у Company");
            using (var db = new AppContextL3_9())
            {
                var companies = db.Companies.ToList();
                foreach (var company in companies)
                {
                    Console.Write($"{company.Name}: ");
                    // При першому зверненні до company.Users проксі підвантажує пов'язаних користувачів
                    foreach (var user in company.Users)
                    {
                        Console.Write($"{user.Name} ");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_9
{
    // Для роботи UseLazyLoadingProxies() класи сутностей ОБОВ'ЯЗКОВО мають бути public (не sealed),
    // а навігаційні властивості — public virtual.

    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public virtual List<User> Users { get; set; } = new();
    }

    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? CompanyId { get; set; }
        public virtual Company? Company { get; set; }
    }

    public class AppContextL3_9 : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_9"));
        }
    }
}

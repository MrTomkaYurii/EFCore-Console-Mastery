using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_8;

    /// <summary>
    /// Урок 3.8: Явне завантаження пов'язаних даних (Explicit Loading) через Load(), Collection() та Reference().
    /// </summary>
    public class Lesson_3_8_ExplicitLoading : ILesson
    {
        public string Id => "3.8";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Явне завантаження (Explicit loading)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.8.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 5;

        public void Run()
        {
            using (var db = new AppContextL3_8())
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

            SqliteDbHelper.PrintSection("1. Явне завантаження через DbSet.Where(...).Load()");
            using (var db = new AppContextL3_8())
            {
                var company = db.Companies.FirstOrDefault(c => c.Name == "Microsoft");
                if (company != null)
                {
                    // Завантажуємо лише користувачів цієї компанії в контекст; EF Core автоматично зв'яже company.Users
                    db.Users.Where(u => u.CompanyId == company.Id).Load();
                    Console.WriteLine($"Компанія: {company.Name}");
                    foreach (var u in company.Users)
                    {
                        Console.WriteLine($"  - {u.Name}");
                    }
                }
            }

            SqliteDbHelper.PrintSection("2. Метод Entry().Collection(...).Load() для колекцій");
            using (var db = new AppContextL3_8())
            {
                var company = db.Companies.FirstOrDefault(c => c.Name == "Google");
                if (company != null)
                {
                    // Можна також застосувати Query() для фільтрації або підрахунку перед Load()
                    var count = db.Entry(company).Collection(c => c.Users).Query().Count();
                    db.Entry(company).Collection(c => c.Users).Load();

                    Console.WriteLine($"Компанія: {company.Name} (кількість: {count})");
                    foreach (var u in company.Users)
                    {
                        Console.WriteLine($"  - {u.Name}");
                    }
                }
            }

            SqliteDbHelper.PrintSection("3. Метод Entry().Reference(...).Load() для одиночної навігаційної властивості");
            using (var db = new AppContextL3_8())
            {
                var user = db.Users.FirstOrDefault(u => u.Name == "Bob");
                if (user != null)
                {
                    db.Entry(user).Reference(u => u.Company).Load();
                    Console.WriteLine($"{user.Name} працює в {user.Company?.Name}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_8
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
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    public class AppContextL3_8 : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_8"));
        }
    }
}

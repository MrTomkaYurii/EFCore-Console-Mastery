using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_1;

    /// <summary>
    /// Урок 3.1: Зовнішні ключі та навігаційні властивості в EF Core.
    /// </summary>
    public class Lesson_3_1_ForeignKeysAndNavigation : ILesson
    {
        public string Id => "3.1";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Зовнішні ключі та навігаційні властивості";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.1.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 1;

        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Встановлення зв'язку через навігаційну властивість");
            using (var db = new AppContextL3_1())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var google = new Company { Name = "Google" };
                var microsoft = new Company { Name = "Microsoft" };

                // Достатньо встановити навігаційну властивість Company — EF Core сам заповнить CompanyId
                var tom = new User { Name = "Tom", Company = google };
                var bob = new User { Name = "Bob", Company = microsoft };
                var alice = new User { Name = "Alice", Company = google };

                db.Companies.AddRange(google, microsoft);
                db.Users.AddRange(tom, bob, alice);
                db.SaveChanges();

                foreach (var user in db.Users.Include(u => u.Company).ToList())
                {
                    Console.WriteLine($"{user.Name} (CompanyId={user.CompanyId}) -> {user.Company?.Name}");
                }
            }

            SqliteDbHelper.PrintSection("2. Встановлення зв'язку через зовнішній ключ (CompanyId)");
            using (var db = new AppContextL3_1())
            {
                // Додаємо нового користувача, вказуючи лише ідентифікатор існуючої компанії
                var microsoft = db.Companies.First(c => c.Name == "Microsoft");
                var kate = new User { Name = "Kate", CompanyId = microsoft.Id };
                db.Users.Add(kate);
                db.SaveChanges();
            }

            // Перевіряємо в новому контексті, що навігаційна властивість підтягується коректно
            using (var db = new AppContextL3_1())
            {
                var kate = db.Users.Include(u => u.Company).First(u => u.Name == "Kate");
                Console.WriteLine($"{kate.Name} (CompanyId={kate.CompanyId}) -> {kate.Company?.Name}");

                Console.WriteLine("\nКомпанії та їхні співробітники:");
                foreach (var comp in db.Companies.Include(c => c.Users).ToList())
                {
                    Console.WriteLine($"- {comp.Name}: {string.Join(", ", comp.Users.Select(u => u.Name))}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_1
{
    /// <summary>
    /// Головна (principal) сутність — компанія.
    /// </summary>
    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        /// <summary>Колекційна навігаційна властивість: усі користувачі компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>
    /// Залежна (dependent) сутність — користувач, що містить зовнішній ключ на Company.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        /// <summary>Зовнішній ключ за конвенцією: &lt;НазваНавігаційноїВластивості&gt;Id.</summary>
        public int CompanyId { get; set; }

        /// <summary>Посилальна навігаційна властивість до головної сутності.</summary>
        public Company? Company { get; set; }
    }

    public class AppContextL3_1 : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_1"));
        }
    }
}

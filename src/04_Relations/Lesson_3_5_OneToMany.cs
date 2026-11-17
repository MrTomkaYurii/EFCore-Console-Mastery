using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_5;

    /// <summary>
    /// Урок 3.5: Відношення один до багатьох (One-to-Many) — повний цикл CRUD.
    /// </summary>
    public class Lesson_3_5_OneToMany : ILesson
    {
        public string Id => "3.5";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Відношення один до багатьох";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.5.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 8;

        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Додавання даних у зв'язку 1:N (Company -> Users)");
            using (var db = new AppContextL3_5())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var microsoft = new Company { Name = "Microsoft" };
                var google = new Company { Name = "Google" };
                db.Companies.AddRange(microsoft, google);

                db.Users.AddRange(
                    new User { Name = "Tom", Company = microsoft },
                    new User { Name = "Bob", Company = microsoft },
                    new User { Name = "Alice", Company = google }
                );
                db.SaveChanges();
            }

            SqliteDbHelper.PrintSection("2. Читання з обох сторін зв'язку");
            using (var db = new AppContextL3_5())
            {
                foreach (var u in db.Users.Include(u => u.Company).ToList())
                {
                    Console.WriteLine($"User: {u.Name} -> Company: {u.Company?.Name}");
                }

                Console.WriteLine();
                foreach (var c in db.Companies.Include(c => c.Users).ToList())
                {
                    Console.WriteLine($"Company: {c.Name} -> [{string.Join(", ", c.Users.Select(u => u.Name))}]");
                }
            }

            SqliteDbHelper.PrintSection("3. Редагування (переведення співробітника в іншу компанію)");
            using (var db = new AppContextL3_5())
            {
                var google = db.Companies.First(c => c.Name == "Google");
                google.Name = "Alphabet";

                var bob = db.Users.First(u => u.Name == "Bob");
                bob.Company = google; // Змінюємо навігаційну властивість -> оновиться CompanyId

                db.SaveChanges();
            }

            SqliteDbHelper.PrintSection("4. Видалення та перевірка результату");
            using (var db = new AppContextL3_5())
            {
                var tom = db.Users.First(u => u.Name == "Tom");
                db.Users.Remove(tom);
                db.SaveChanges();

                foreach (var c in db.Companies.Include(c => c.Users).ToList())
                {
                    Console.WriteLine($"Company: {c.Name} -> [{string.Join(", ", c.Users.Select(u => u.Name))}]");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_5
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
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    public class AppContextL3_5 : DbContext
    {
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_5"));
        }
    }
}

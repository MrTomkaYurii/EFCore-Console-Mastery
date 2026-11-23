using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_5;

    /// <summary>
    /// Урок 4.8: Відношення один до багатьох (One-to-Many) — повний цикл CRUD.
    /// </summary>
    public class Lesson_4_08_OneToMany : ILesson
    {
        public string Id => "4.8";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Відношення один до багатьох";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.5.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 8;

        /// <summary>
        /// Демонструє повний цикл CRUD (створення, читання з обох сторін, переприв'язку та видалення)
        /// для зв'язку один-до-багатьох (1:N).
        /// </summary>
        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Додавання даних у зв'язку 1:N (Company -> Users)");
            using (var db = new AppContextL3_5())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var microsoft = new Company { Name = "Microsoft" };
                var google = new Company { Name = "Google" };
                db.Companies.AddRange(microsoft, google);

                // Кожному User вказуємо навігаційну властивість Company; EF Core виконає INSERT у Companies першим,
                // отримає згенеровані Id через RETURNING / last_insert_rowid() у SQLite та підставить їх у Users.CompanyId.
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
                // З боку залежної сутності (User -> Company): через обов'язковий FK генерується INNER JOIN.
                foreach (var u in db.Users.Include(u => u.Company).ToList())
                {
                    Console.WriteLine($"User: {u.Name} -> Company: {u.Company?.Name}");
                }

                Console.WriteLine();
                // З боку головної сутності (Company -> Users): генерується LEFT JOIN, щоб показати компанії навіть без співробітників.
                foreach (var c in db.Companies.Include(c => c.Users).ToList())
                {
                    Console.WriteLine($"Company: {c.Name} -> [{string.Join(", ", c.Users.Select(u => u.Name))}]");
                }
            }

            SqliteDbHelper.PrintSection("3. Редагування (переведення співробітника в іншу компанію)");
            using (var db = new AppContextL3_5())
            {
                var google = db.Companies.First(c => c.Name == "Google");
                // Змінюємо назву компанії — Change Tracker позначає лише властивість Name як Modified.
                google.Name = "Alphabet";

                var bob = db.Users.First(u => u.Name == "Bob");
                // Змінюємо посилання bob.Company: під час DetectChanges() EF Core бачить зміну зв'язку
                // і автоматично оновлює зовнішній ключ CompanyId у SQL-запиті UPDATE "Users" SET "CompanyId" = @p0.
                bob.Company = google;

                db.SaveChanges();
            }

            SqliteDbHelper.PrintSection("4. Видалення та перевірка результату");
            using (var db = new AppContextL3_5())
            {
                var tom = db.Users.First(u => u.Name == "Tom");
                // Видалення залежної сутності User не зачіпає головну сутність Company.
                db.Users.Remove(tom);
                db.SaveChanges();

                // Microsoft залишиться без користувачів (порожній список), бо Tom видалений, а Bob переведений в Alphabet.
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
    /// <summary>Головна (principal) сутність — компанія у відношенні 1:N.</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Колекція співробітників компанії (сторона "багато").</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Залежна (dependent) сутність — співробітник у відношенні 1:N.</summary>
    public class User
    {
        /// <summary>Первинний ключ співробітника.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я співробітника.</summary>
        public string? Name { get; set; }

        /// <summary>Обов'язковий зовнішній ключ компанії (NOT NULL у SQLite).</summary>
        public int CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії (сторона "один").</summary>
        public Company? Company { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 3.5 (SQLite).</summary>
    public class AppContextL3_5 : DbContext
    {
        /// <summary>Компанії.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Співробітники.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_5"));
        }
    }
}

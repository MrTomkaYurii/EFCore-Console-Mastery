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

        /// <summary>
        /// Демонструє явне завантаження пов'язаних даних (Explicit Loading) за вимогою:
        /// через <c>DbSet.Where(...).Load()</c>, <c>Entry().Collection(...).Load()</c> та <c>Entry().Reference(...).Load()</c>.
        /// </summary>
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
                    // Load() виконує окремий SQL-запит SELECT ... FROM Users WHERE CompanyId = @p0
                    // і поміщає знайдених користувачів у Change Tracker.
                    // Завдяки механізму Relationship Fixup EF Core автоматично заповнює колекцію company.Users у пам'яті.
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
                    // Entry(company).Collection(...) дає доступ до метаданих навігаційної колекції.
                    // Query() дозволяє виконати агрегацію (Count) або фільтрацію (Where) на боці SQLite БЕЗ завантаження всіх рядків у пам'ять,
                    // а Load() завантажує самі об'єкти в колекцію company.Users.
                    // Зауваження: Explicit Loading вимагає, щоб головна сутність відстежувалася (tracking), тобто не можна використовувати AsNoTracking().
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
                    // Для посилальної (одиночної) навігаційної властивості використовується Reference().Load(), а не Collection().
                    // Це виконує окремий запит SELECT ... FROM Companies WHERE Id = @p0.
                    db.Entry(user).Reference(u => u.Company).Load();
                    Console.WriteLine($"{user.Name} працює в {user.Company?.Name}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_8
{
    /// <summary>Компанія (головна сутність).</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Колекція співробітників компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Співробітник (залежна сутність із необов'язковим FK).</summary>
    public class User
    {
        /// <summary>Первинний ключ співробітника.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я співробітника.</summary>
        public string? Name { get; set; }

        /// <summary>Необов'язковий зовнішній ключ компанії.</summary>
        public int? CompanyId { get; set; }

        /// <summary>Посилальна навігаційна властивість до компанії.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 3.8 (SQLite).</summary>
    public class AppContextL3_8 : DbContext
    {
        /// <summary>Співробітники.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Компанії.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_8"));
        }
    }
}

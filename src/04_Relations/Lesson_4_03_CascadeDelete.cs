using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_2;

    /// <summary>
    /// Урок 4.3: Каскадне видалення (Cascade Delete) та інші стратегії OnDelete.
    /// </summary>
    public class Lesson_4_03_CascadeDelete : ILesson
    {
        public string Id => "4.3";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Каскадне видалення";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.2.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 3;

        /// <summary>
        /// Демонструє різницю між каскадним видаленням (<c>ON DELETE CASCADE</c>) для обов'язкових зв'язків
        /// та обнуленням зовнішнього ключа (<c>ON DELETE SET NULL</c>) для необов'язкових зв'язків.
        /// </summary>
        public void Run()
        {
            SqlServerDbHelper.PrintSection("1. Обов'язковий зв'язок -> ON DELETE CASCADE");
            using (var db = new AppContextL3_2())
            {
                SqlServerDbHelper.RecreateSchema(db, "l4_03");

                var microsoft = new Company { Name = "Microsoft" };
                var google = new Company { Name = "Google" };
                db.Companies.AddRange(microsoft, google);

                // Обов'язковий зв'язок (User.CompanyId не nullable): у SQLite генерується ON DELETE CASCADE.
                // Видалення Company автоматично видалить усі пов'язані рядки Users як у пам'яті (якщо завантажені), так і в самій БД.
                db.Users.AddRange(
                    new User { Name = "Tom", Company = microsoft },
                    new User { Name = "Alice", Company = microsoft },
                    new User { Name = "Bob", Company = google },
                    new User { Name = "Kate", Company = google }
                );

                // Необов'язковий зв'язок з DeleteBehavior.SetNull: у SQLite генерується ON DELETE SET NULL.
                // Без явного OnDelete(DeleteBehavior.SetNull) за замовчуванням для nullable FK діє ClientSetNull (у БД було б ON DELETE NO ACTION/RESTRICT,
                // і без завантаження Contractors у контекст видалення Company впало б через порушення FK у SQLite).
                db.Contractors.AddRange(
                    new Contractor { Name = "Freelancer John", Company = microsoft },
                    new Contractor { Name = "Freelancer Sara", Company = google }
                );

                db.SaveChanges();

                Console.WriteLine("До видалення Microsoft:");
                Console.WriteLine($"  Users: {string.Join(", ", db.Users.Select(u => u.Name))}");
                Console.WriteLine($"  Contractors: {string.Join(", ", db.Contractors.Select(c => $"{c.Name} (CompanyId={c.CompanyId})"))}");

                // Видаляємо компанію Microsoft: SQLite виконує DELETE FROM "Companies" WHERE "Id" = @p0,
                // а тригери зовнішніх ключів БД каскадно видаляють Users та обнуляють Contractors.CompanyId.
                db.Companies.Remove(microsoft);
                db.SaveChanges();
            }

            // Відкриваємо новий контекст, щоб переконатися, що зміни відбулися безпосередньо в таблицях SQLite.
            using (var db = new AppContextL3_2())
            {
                Console.WriteLine("\nПісля видалення Microsoft:");
                Console.WriteLine($"  Users (Tom та Alice видалені каскадно): {string.Join(", ", db.Users.Select(u => u.Name))}");
                Console.WriteLine("  Contractors (Freelancer John залишився з CompanyId=NULL):");
                foreach (var c in db.Contractors.ToList())
                {
                    Console.WriteLine($"    - {c.Name}: CompanyId={c.CompanyId?.ToString() ?? "NULL"}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_2
{
    /// <summary>
    /// Головна сутність — компанія, на яку посилаються штатні користувачі та підрядники.
    /// </summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Штатні користувачі компанії (обов'язковий зв'язок, каскадне видалення).</summary>
        public List<User> Users { get; set; } = new();

        /// <summary>Зовнішні підрядники компанії (необов'язковий зв'язок, обнулення FK).</summary>
        public List<Contractor> Contractors { get; set; } = new();
    }

    /// <summary>
    /// Штатний користувач: обов'язковий зовнішній ключ (<c>int</c>) -> каскадне видалення (<c>DeleteBehavior.Cascade</c>) за замовчуванням.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Обов'язковий зовнішній ключ (NOT NULL у SQLite).</summary>
        public int CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Підрядник: необов'язковий зовнішній ключ (<c>int?</c>) -> за конвенцією <c>ClientSetNull</c>,
    /// але тут явно налаштовано <c>DeleteBehavior.SetNull</c> для рівня БД.
    /// </summary>
    public class Contractor
    {
        /// <summary>Первинний ключ підрядника.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я підрядника.</summary>
        public string? Name { get; set; }

        /// <summary>Необов'язковий зовнішній ключ (NULL у SQLite).</summary>
        public int? CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Контекст бази даних для уроку 3.2 (SQLite).
    /// </summary>
    public class AppContextL3_2 : DbContext
    {
        /// <summary>Штатні користувачі.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Підрядники.</summary>
        public DbSet<Contractor> Contractors { get; set; } = null!;

        /// <summary>Компанії.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        /// <summary>
        /// Налаштовує поведінку видалення через Fluent API.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l4_03");
            // Явно задаємо DeleteBehavior.SetNull для Contractor, щоб у DDL таблиці Contractors було ON DELETE SET NULL.
            // Примітка: у SQL Server множинні каскадні шляхи (multiple cascade paths) часто заборонені і викликають помилку,
            // тоді як у SQLite така конфігурація працює без обмежень на кількість шляхів.
            modelBuilder.Entity<Contractor>()
                .HasOne(c => c.Company)
                .WithMany(comp => comp.Contractors)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

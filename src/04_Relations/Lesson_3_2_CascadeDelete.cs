using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_2;

    /// <summary>
    /// Урок 3.2: Каскадне видалення (Cascade Delete) та інші стратегії OnDelete.
    /// </summary>
    public class Lesson_3_2_CascadeDelete : ILesson
    {
        public string Id => "3.2";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Каскадне видалення";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.2.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 3;

        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Обов'язковий зв'язок -> ON DELETE CASCADE");
            using (var db = new AppContextL3_2())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var microsoft = new Company { Name = "Microsoft" };
                var google = new Company { Name = "Google" };
                db.Companies.AddRange(microsoft, google);

                // Обов'язковий зв'язок (User.CompanyId не nullable) -> видалення Company видалить її Users
                db.Users.AddRange(
                    new User { Name = "Tom", Company = microsoft },
                    new User { Name = "Alice", Company = microsoft },
                    new User { Name = "Bob", Company = google },
                    new User { Name = "Kate", Company = google }
                );

                // Необов'язковий зв'язок з DeleteBehavior.SetNull -> при видаленні Company поле CompanyId стане NULL
                db.Contractors.AddRange(
                    new Contractor { Name = "Freelancer John", Company = microsoft },
                    new Contractor { Name = "Freelancer Sara", Company = google }
                );

                db.SaveChanges();

                Console.WriteLine("До видалення Microsoft:");
                Console.WriteLine($"  Users: {string.Join(", ", db.Users.Select(u => u.Name))}");
                Console.WriteLine($"  Contractors: {string.Join(", ", db.Contractors.Select(c => $"{c.Name} (CompanyId={c.CompanyId})"))}");

                // Видаляємо компанію Microsoft
                db.Companies.Remove(microsoft);
                db.SaveChanges();
            }

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
    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
        public List<Contractor> Contractors { get; set; } = new();
    }

    /// <summary>Обов'язковий зовнішній ключ (int) -> каскадне видалення за замовчуванням.</summary>
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    /// <summary>Необов'язковий зовнішній ключ (int?) -> при видаленні компанії стає NULL (ClientSetNull/SetNull).</summary>
    public class Contractor
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    public class AppContextL3_2 : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Contractor> Contractors { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_2"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Явно задаємо SetNull для Contractor, щоб на рівні БД було ON DELETE SET NULL
            modelBuilder.Entity<Contractor>()
                .HasOne(c => c.Company)
                .WithMany(comp => comp.Contractors)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

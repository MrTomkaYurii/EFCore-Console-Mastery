using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.3: Визначення моделей.
/// Показує, як сутності потрапляють у модель EF Core (DbSet, навігаційні властивості,
/// modelBuilder.Entity&lt;T&gt;()) і як виключати типи через Ignore&lt;T&gt;() або [NotMapped].
/// </summary>
public class Lesson_2_3_DefiningModels : ILesson
{
    public string Id => "2.3";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Визначення моделей";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.3.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 2;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.3. Визначення моделей");

        // Частина 1: Автоматичне включення через DbSet, навігаційні властивості та Entity<T>()
        using (var db = new L2_3.InclusionContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            var company = new L2_3.Company { Name = "TechCorp" };
            var user = new L2_3.User { Name = "Tom", Age = 30, Company = company };
            db.Users.Add(user);
            db.Set<L2_3.Country>().Add(new L2_3.Country { Name = "Ukraine" });
            db.SaveChanges();

            var tables = db.Database
                .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
                .ToList();

            Console.WriteLine("Таблиці в InclusionContext (Users + Company через навігацію + Country через Entity<T>()):");
            foreach (var t in tables)
                Console.WriteLine($"  - {t}");
        }

        // Частина 2: Виключення сутностей через Ignore<T>() та [NotMapped]
        using (var db = new L2_3.ExclusionContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            db.Users.Add(new L2_3.UserWithExcluded { Name = "Alice", Age = 25 });
            db.SaveChanges();

            var tables = db.Database
                .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
                .ToList();

            Console.WriteLine("\nТаблиці в ExclusionContext (тільки Users, бо TempBadge має [NotMapped], а AuditLog виключено через Ignore):");
            foreach (var t in tables)
                Console.WriteLine($"  - {t}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_3
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }

        // Навігаційна властивість: автоматично включає Company у модель, навіть без DbSet<Company>
        public Company? Company { get; set; }
    }

    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class Country
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class InclusionContext : DbContext
    {
        // Обидва варіанти ініціалізації DbSet запобігають попередженню CS8618 (nullable reference types)
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_3_inc"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Явне включення сутності Country без оголошення DbSet<Country>
            modelBuilder.Entity<Country>();
        }
    }

    // Демонстрація виключення сутностей із моделі
    [NotMapped]
    public class TempBadge
    {
        public int Code { get; set; }
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public string? Message { get; set; }
    }

    public class UserWithExcluded
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }

        // Не створить таблицю чи зовнішній ключ, бо TempBadge позначений [NotMapped]
        public TempBadge? Badge { get; set; }

        // Не створить таблицю чи зовнішній ключ, бо AuditLog виключено через modelBuilder.Ignore<AuditLog>()
        public AuditLog? LastAudit { get; set; }
    }

    public class ExclusionContext : DbContext
    {
        // Альтернативний спосіб оголошення DbSet без null!
        public DbSet<UserWithExcluded> Users => Set<UserWithExcluded>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_3_exc"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<AuditLog>();
        }
    }
}

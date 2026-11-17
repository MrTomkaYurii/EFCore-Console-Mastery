using System.ComponentModel.DataAnnotations;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.7: Налаштування ключів.
/// Демонструє первинні ключі за угодою, [Key], HasKey/HasName,
/// складені первинні ключі та альтернативні (унікальні) ключі HasAlternateKey.
/// </summary>
public class Lesson_2_7_Keys : ILesson
{
    public string Id => "2.7";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Налаштування ключів";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.7.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 8;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.7. Налаштування ключів");

        // 1. Первинний ключ через Fluent API (HasKey + HasName) та альтернативний ключ (HasAlternateKey)
        using (var db = new L2_7.SingleAndAlternateKeyContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            var sql = db.Database
                .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Users'")
                .FirstOrDefault();

            Console.WriteLine("1) DDL таблиці Users (HasKey + HasName + HasAlternateKey):");
            Console.WriteLine(sql);

            db.Users.Add(new L2_7.User { Ident = 1, Name = "Tom", Passport = "AB123456" });
            db.SaveChanges();

            // Очищаємо ChangeTracker, щоб перевірити саме обмеження UNIQUE на рівні БД
            db.ChangeTracker.Clear();

            // Перевірка унікальності альтернативного ключа Passport
            try
            {
                db.Users.Add(new L2_7.User { Ident = 2, Name = "Bob", Passport = "AB123456" });
                db.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nСпроба дублювання альтернативного ключа Passport заблокована БД:\n  {ex.InnerException?.Message}");
            }
        }

        // 2. Складений первинний ключ (лише через Fluent API)
        using (var db = new L2_7.CompositeKeyContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            var sql = db.Database
                .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Citizens'")
                .FirstOrDefault();

            Console.WriteLine("\n2) DDL таблиці Citizens (складений первинний ключ):");
            Console.WriteLine(sql);

            db.Citizens.AddRange(
                new L2_7.Citizen { PassportSeria = "1234", PassportNumber = "345678", Name = "Tom" },
                new L2_7.Citizen { PassportSeria = "1234", PassportNumber = "345679", Name = "Bob" }
            );
            db.SaveChanges();

            Console.WriteLine("Збережені громадяни зі складеним ключем:");
            foreach (var c in db.Citizens.AsNoTracking().ToList())
            {
                Console.WriteLine($"  {c.Name}: {c.PassportSeria} {c.PassportNumber}");
            }
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_7
{
    public class User
    {
        // Нестандартне ім'я ключа: у SingleAndAlternateKeyContext налаштоване через HasKey
        public int Ident { get; set; }
        public string? Name { get; set; }

        // Альтернативний ключ (отримає обмеження UNIQUE у БД)
        public string? Passport { get; set; }
    }

    public class AnnotatedProduct
    {
        // Альтернатива HasKey — атрибут [Key]
        [Key]
        public int Code { get; set; }
        public string? Title { get; set; }
    }

    public class SingleAndAlternateKeyContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<AnnotatedProduct> Products => Set<AnnotatedProduct>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_7_single"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.Ident)
                .HasName("UsersPrimaryKey");

            modelBuilder.Entity<User>()
                .HasAlternateKey(u => u.Passport);
        }
    }

    public class Citizen
    {
        public string PassportSeria { get; set; } = string.Empty;
        public string PassportNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    public class CompositeKeyContext : DbContext
    {
        public DbSet<Citizen> Citizens => Set<Citizen>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_7_comp"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Складений первинний ключ можна задати тільки через Fluent API
            modelBuilder.Entity<Citizen>()
                .HasKey(c => new { c.PassportSeria, c.PassportNumber });
        }
    }
}

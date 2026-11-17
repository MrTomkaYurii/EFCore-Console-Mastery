using System.ComponentModel.DataAnnotations;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.9: Обмеження властивостей.
/// Демонструє CHECK-обмеження (HasCheckConstraint) та обмеження довжини рядків
/// ([MaxLength], HasMaxLength), а також пояснює їхню поведінку в SQLite.
/// </summary>
public class Lesson_2_9_PropertyConstraints : ILesson
{
    public string Id => "2.9";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Обмеження властивостей";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.9.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 11;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.9. Обмеження властивостей");

        using var db = new L2_9.ApplicationContext();
        SqliteDbHelper.RecreateDatabase(db);

        var sql = db.Database
            .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Users'")
            .FirstOrDefault();

        Console.WriteLine("DDL таблиці Users у SQLite:");
        Console.WriteLine(sql);

        // 1. Успішне додавання валідного користувача
        db.Users.Add(new L2_9.User { Name = "Tom", Email = "tom@example.com", Age = 30 });
        db.SaveChanges();
        Console.WriteLine("\nКористувача Tom (Age=30) успішно збережено.");

        // 2. Порушення CHECK-обмеження ValidAge (Age > 0 AND Age < 120)
        try
        {
            db.Users.Add(new L2_9.User { Name = "Bob", Email = "bob@example.com", Age = 123 });
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"\nОчікувана помилка при Age=123 (порушення CHECK \"ValidAge\"):\n  {ex.InnerException?.Message}");
        }

        // Від'єднуємо невалідного Bob, щоб наступний SaveChanges() не намагався зберегти його знову
        db.ChangeTracker.Clear();

        // 3. Демонстрація MaxLength у SQLite: SQLite ігнорує довжину TEXT і зберігає рядок > 10 символів
        var longName = "ДужеДовгеІм'яПонадДесятьСимволів";
        db.Users.Add(new L2_9.User { Name = longName, Email = "long@example.com", Age = 25 });
        db.SaveChanges();
        Console.WriteLine($"\nУвага (особливість SQLite): рядок довжиною {longName.Length} символів збережено попри [MaxLength(10)]!");
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_9
{
    public class User
    {
        public int Id { get; set; }

        // Анотація [MaxLength(10)] у SQL Server створить nvarchar(10), але в SQLite стовпець лишиться TEXT
        [MaxLength(10)]
        public string? Name { get; set; }

        // Налаштовано через Fluent API HasMaxLength(50)
        public string? Email { get; set; }

        public int Age { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_9"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Обмеження CHECK на рівні таблиці (працює і в SQLite, і в SQL Server)
            modelBuilder.Entity<User>()
                .ToTable(t => t.HasCheckConstraint("ValidAge", "Age > 0 AND Age < 120"));

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(50);
        }
    }
}

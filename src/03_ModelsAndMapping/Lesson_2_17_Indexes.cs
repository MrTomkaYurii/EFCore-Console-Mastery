using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.17: Налаштування індексів.
/// Демонструє створення звичайних, унікальних, складених та фільтрованих індексів
/// за допомогою атрибута [Index] та Fluent API (HasIndex, IsUnique, HasDatabaseName, HasFilter).
/// </summary>
public class Lesson_2_17_Indexes : ILesson
{
    public string Id => "2.17";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Налаштування індексів";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.17.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 9;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.17. Налаштування індексів");

        using var db = new L2_17.ApplicationContext();
        SqliteDbHelper.RecreateDatabase(db);

        // Переглядаємо всі створені індекси в SQLite через sqlite_master
        var indexes = db.Database
            .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='index' AND sql IS NOT NULL ORDER BY name")
            .ToList();

        Console.WriteLine("Створені індекси в SQLite:");
        foreach (var idxSql in indexes)
        {
            Console.WriteLine($"  {idxSql}");
        }

        // Додаємо записи: PhoneNumber може бути NULL у кількох користувачів завдяки фільтрованому унікальному індексу
        db.Users.AddRange(
            new L2_17.User { Name = "Tom", Passport = "P-100", PhoneNumber = "+380501112233" },
            new L2_17.User { Name = "Alice", Passport = "P-200", PhoneNumber = null },
            new L2_17.User { Name = "Bob", Passport = "P-300", PhoneNumber = null }
        );
        db.SaveChanges();
        Console.WriteLine("\nУспішно додано двох користувачів з PhoneNumber = NULL (фільтрований індекс не блокує NULL).");

        // Перевірка унікальності індексу на Passport
        try
        {
            db.Users.Add(new L2_17.User { Name = "Clone", Passport = "P-100", PhoneNumber = "+380509998877" });
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"\nДубль за унікальним індексом Passport заблоковано:\n  {ex.InnerException?.Message}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_17
{
    // Приклад налаштування індексу через атрибут [Index] (з простору Microsoft.EntityFrameworkCore)
    [Index(nameof(PhoneNumber), Name = "Attr_Phone_Index")]
    [Index(nameof(PhoneNumber), nameof(Passport))] // Складений індекс через атрибут
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Passport { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_17"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Унікальний індекс на Passport через Fluent API
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Passport)
                .IsUnique()
                .HasDatabaseName("IX_Users_Passport_Unique");

            // Фільтрований (частковий) унікальний індекс у SQLite:
            // індексує та перевіряє унікальність лише для непорожніх номерів телефону
            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique()
                .HasDatabaseName("IX_Users_Phone_NotNull")
                .HasFilter("\"PhoneNumber\" IS NOT NULL");
        }
    }
}

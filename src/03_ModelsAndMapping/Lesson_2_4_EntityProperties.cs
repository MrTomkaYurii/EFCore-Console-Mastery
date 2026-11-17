using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.4: Властивості сутності.
/// Демонструє автоматичне зіставлення публічних властивостей, їх виключення через
/// [NotMapped] та Ignore(), а також властивості з явним backing field (get/set).
/// </summary>
public class Lesson_2_4_EntityProperties : ILesson
{
    public string Id => "2.4";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Властивості сутності";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.4.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 3;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.4. Властивості сутності");

        using var db = new L2_4.ApplicationContext();
        SqliteDbHelper.RecreateDatabase(db);

        var user = new L2_4.User
        {
            Name = "Tom",
            Age = 35,
            Address = "Тимчасова адреса (не зберігається в БД)",
            SessionToken = "secret-token-123"
        };
        db.Users.Add(user);
        db.SaveChanges();

        // Перевіряємо, які стовпці реально створено в таблиці Users
        var createSql = db.Database
            .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Users'")
            .FirstOrDefault();

        Console.WriteLine("DDL таблиці Users (без Address і SessionToken):");
        Console.WriteLine(createSql);

        // Читаємо з БД у новому запиті без кешування (AsNoTracking)
        var fromDb = db.Users.AsNoTracking().Single();
        Console.WriteLine($"\nПрочитано з БД: Id={fromDb.Id}, Name={fromDb.Name}, Age={fromDb.Age}");
        Console.WriteLine($"Виключені властивості після читання з БД: Address='{fromDb.Address ?? "null"}', SessionToken='{fromDb.SessionToken ?? "null"}'");
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_4
{
    public class User
    {
        // Повноцінна властивість із приватним полем (backing field)
        private string _name = string.Empty;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set => _name = value.Trim();
        }

        public int Age { get; set; }

        // Виключення властивості за допомогою Fluent API (див. OnModelCreating)
        public string? Address { get; set; }

        // Виключення властивості за допомогою анотації даних [NotMapped]
        [NotMapped]
        public string? SessionToken { get; set; }

        // Властивість лише для читання (без set) за замовчуванням не зіставляється зі стовпцем
        public string Summary => $"{Name} ({Age} р.)";
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_4"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Виключаємо властивість Address із моделі
            modelBuilder.Entity<User>().Ignore(u => u.Address);
        }
    }
}

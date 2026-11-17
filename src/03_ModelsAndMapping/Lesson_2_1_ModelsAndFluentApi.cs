using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.1: Моделі, Fluent API та анотації даних.
/// Демонструє три підходи до конфігурації моделі в EF Core:
/// умовні угоди (conventions), анотації даних (Data Annotations) та Fluent API.
/// </summary>
public class Lesson_2_1_ModelsAndFluentApi : ILesson
{
    public string Id => "2.1";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Моделі, Fluent API та анотації даних";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.1.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 1;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.1. Моделі, Fluent API та анотації даних");

        using var db = new L2_1.ApplicationContext();
        SqliteDbHelper.RecreateDatabase(db);

        // Додаємо користувачів: для Id спрацьовує автогенерація,
        // а відображення на стовпці таблиці налаштоване через [Column] та Fluent API.
        db.Users.AddRange(
            new L2_1.User { Name = "Tom", Age = 33 },
            new L2_1.User { Name = "Alice", Age = 26 }
        );
        db.SaveChanges();

        // Перевіряємо згенеровану схему таблиці Users безпосередньо в SQLite
        var createSql = db.Database
            .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Users'")
            .FirstOrDefault();

        Console.WriteLine("DDL таблиці Users у SQLite:");
        Console.WriteLine(createSql);

        Console.WriteLine("\nЗбережені користувачі:");
        foreach (var user in db.Users.AsNoTracking().ToList())
        {
            Console.WriteLine($"  user_id={user.Id}, Name={user.Name}, Age={user.Age}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_1
{
    /// <summary>
    /// Сутність User, де поєднано умовні угоди (Name, Age)
    /// та анотацію даних [Column("user_id")] для властивості Id.
    /// </summary>
    public class User
    {
        // Анотація даних: перейменовує стовпець Id -> user_id у БД
        [Column("user_id")]
        public int Id { get; set; }

        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_1"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Точка входу для Fluent API: тут можна перевизначити угоди та анотації
            modelBuilder.Entity<User>()
                .Property(u => u.Name)
                .HasDefaultValue("Anonymous");

            base.OnModelCreating(modelBuilder);
        }
    }
}

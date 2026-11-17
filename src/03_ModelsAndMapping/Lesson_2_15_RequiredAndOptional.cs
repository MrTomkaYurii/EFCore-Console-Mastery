using System.ComponentModel.DataAnnotations;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.15: Обов'язкові та необов'язкові властивості.
/// Показує, як nullable-типи в C#, атрибут [Required] та метод IsRequired()
/// впливають на обмеження NOT NULL у таблицях бази даних.
/// </summary>
public class Lesson_2_15_RequiredAndOptional : ILesson
{
    public string Id => "2.15";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Обов'язкові та необов'язкові властивості";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.15.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 7;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.15. Обов'язкові та необов'язкові властивості");

        using var db = new L2_15.ApplicationContext();
        SqliteDbHelper.RecreateDatabase(db);

        var sql = db.Database
            .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Users'")
            .FirstOrDefault();

        Console.WriteLine("DDL таблиці Users у SQLite:");
        Console.WriteLine(sql);

        // Коректне додавання користувача з усіма обов'язковими полями
        db.Users.Add(new L2_15.User
        {
            Name = "Tom",
            Email = "tom@example.com",
            Login = "tom_dev",
            Company = null, // Необов'язкова властивість
            Age = 28
        });
        db.SaveChanges();
        Console.WriteLine("\nКористувача Tom успішно збережено (Company = NULL дозволено).");

        // Спроба зберегти користувача без обов'язкової властивості Email ([Required])
        try
        {
            db.Users.Add(new L2_15.User
            {
                Name = "Bob",
                Email = null,   // Порушує [Required] -> NOT NULL у БД
                Login = "bob"
            });
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"\nОчікувана помилка DbUpdateException при Email = null:\n  {ex.InnerException?.Message ?? ex.Message}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_15
{
    public class User
    {
        public int Id { get; set; }

        // Не-nullable тип у C# -> автоматично NOT NULL у БД
        public string Name { get; set; } = string.Empty;

        // Nullable-тип, але з атрибутом [Required] -> стає NOT NULL у БД
        [Required]
        public string? Email { get; set; }

        // Nullable-тип, налаштований як обов'язковий через Fluent API IsRequired()
        public string? Login { get; set; }

        // Nullable-тип без обмежень -> допускає NULL у БД
        public string? Company { get; set; }

        // Значимий тип int (не int?) -> NOT NULL у БД
        public int Age { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_15"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(u => u.Login)
                .IsRequired();
        }
    }
}

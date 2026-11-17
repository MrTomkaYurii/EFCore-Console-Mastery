using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.8: Генерація значень властивостей і стовпців.
/// Демонструє автогенерацію ключів, відключення автогенерації ([DatabaseGenerated(None)] / ValueGeneratedNever),
/// значення за замовчуванням (HasDefaultValue, HasDefaultValueSql) та обчислювані стовпці (HasComputedColumnSql).
/// </summary>
public class Lesson_2_8_ValueGeneration : ILesson
{
    public string Id => "2.8";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Генерація значень властивостей і стовпців";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.8.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 10;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.8. Генерація значень властивостей і стовпців");

        // Частина 1: HasDefaultValue, HasDefaultValueSql("DATETIME('now')") та HasComputedColumnSql
        using (var db = new L2_8.DefaultsAndComputedContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            var sql = db.Database
                .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Users'")
                .FirstOrDefault();

            Console.WriteLine("1) DDL таблиці Users (DEFAULT 18, DEFAULT DATETIME('now'), GENERATED ALWAYS AS):");
            Console.WriteLine(sql);

            var user = new L2_8.User
            {
                FirstName = "Tom",
                LastName = "Smith"
                // Age та CreatedAt не задаємо — спрацюють значення за замовчуванням у БД
            };

            Console.WriteLine($"\nДо SaveChanges: Id={user.Id}, Age={user.Age}, CreatedAt={user.CreatedAt}, FullName='{user.FullName ?? "null"}'");
            db.Users.Add(user);
            db.SaveChanges();
            Console.WriteLine($"Після SaveChanges: Id={user.Id}, Age={user.Age}, CreatedAt={user.CreatedAt:yyyy-MM-dd HH:mm:ss}, FullName='{user.FullName}'");
        }

        // Частина 2: Відключення автогенерації ключа через ValueGeneratedNever() та [DatabaseGenerated(None)]
        using (var db = new L2_8.ManualKeyContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            db.ManualUsers.AddRange(
                new L2_8.ManualUser { Id = 101, Name = "Alice" },
                new L2_8.ManualUser { Id = 205, Name = "Bob" }
            );
            db.SaveChanges();

            Console.WriteLine("\n2) Користувачі з ключами, заданими вручну (ValueGeneratedNever):");
            foreach (var u in db.ManualUsers.AsNoTracking().ToList())
            {
                Console.WriteLine($"  Id={u.Id}, Name={u.Name}");
            }
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_8
{
    public class User
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Обчислюваний стовпець (лише get)
        public string? FullName { get; }

        public int Age { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DefaultsAndComputedContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_8_def"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Фіксоване значення за замовчуванням
            modelBuilder.Entity<User>()
                .Property(u => u.Age)
                .HasDefaultValue(18);

            // SQL-вираз для значення за замовчуванням у SQLite (замість GETDATE() у SQL Server)
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("DATETIME('now')");

            // Обчислюваний стовпець у синтаксисі SQLite (конкатенація через || замість +)
            modelBuilder.Entity<User>()
                .Property(u => u.FullName)
                .HasComputedColumnSql("FirstName || ' ' || LastName");
        }
    }

    public class ManualUser
    {
        // Можна також використовувати атрибут [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class ManualKeyContext : DbContext
    {
        public DbSet<ManualUser> ManualUsers => Set<ManualUser>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_8_man"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Дублюємо через Fluent API для наочності
            modelBuilder.Entity<ManualUser>()
                .Property(u => u.Id)
                .ValueGeneratedNever();
        }
    }
}

using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch02;

/// <summary>
/// Урок 2.1: Провайдер MS SQL Server (Docker або LocalDB через appsettings.json).
/// </summary>
public class Lesson_2_01_SqlServerProvider : ILesson
{
    public string Id => "2.1";
    public string Chapter => "02. Провайдери баз даних";
    public string Title => "Провайдер MS SQL Server";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/7.1.php";
    public int ChapterOrder => 2;
    public int LessonOrder => 1;

    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l2_01");
        }
    }

    public void Run()
    {
        var connStr = SqlServerDbHelper.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlServer(connStr)
            .Options;

        SqlServerDbHelper.PrintSection("1. Налаштування UseSqlServer та перевірка провайдера");
        using (var db = new ApplicationContext(options))
        {
            SqlServerDbHelper.RecreateSchema(db, "l2_01");
            Console.WriteLine($"Активний профіль: {SqlServerDbHelper.ActiveConnectionName}");
            Console.WriteLine($"Провайдер:        {db.Database.ProviderName}");

            db.Users.AddRange(
                new User { Name = "Tom", Age = 33 },
                new User { Name = "Alice", Age = 26 });
            db.SaveChanges();
        }

        SqlServerDbHelper.PrintSection("2. Отримання даних із MS SQL Server");
        using (var db = new ApplicationContext(options))
        {
            foreach (var u in db.Users.AsNoTracking().ToList())
                Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");

            SqlServerDbHelper.PrintSection("3. Згенерований T-SQL (ToQueryString)");
            Console.WriteLine(db.Users.Where(u => u.Age > 25).ToQueryString());
        }
    }
}

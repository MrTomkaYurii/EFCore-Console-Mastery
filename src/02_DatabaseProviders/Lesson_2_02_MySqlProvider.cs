using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch02;

/// <summary>
/// Урок 2.2: Провайдер MySQL (MySql.EntityFrameworkCore + Docker-контейнер MySql).
/// </summary>
public class Lesson_2_02_MySqlProvider : ILesson
{
    public string Id => "2.2";
    public string Chapter => "02. Провайдери баз даних";
    public string Title => "Провайдер MySQL";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/7.2.php";
    public int ChapterOrder => 2;
    public int LessonOrder => 2;

    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class MySqlAppContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public MySqlAppContext(DbContextOptions<MySqlAppContext> options) : base(options) { }
    }

    public void Run()
    {
        var connStr = SqlServerDbHelper.GetConnectionString("MySql");
        var options = new DbContextOptionsBuilder<MySqlAppContext>()
            .UseMySQL(connStr)
            .Options;

        SqlServerDbHelper.PrintSection("1. Конфігурація провайдера MySQL (UseMySQL)");
        using var db = new MySqlAppContext(options);
        Console.WriteLine($"Провайдер: {db.Database.ProviderName}");

        if (!db.Database.CanConnect())
        {
            Console.WriteLine("Контейнер MySQL зараз зупинений або недоступний.");
            Console.WriteLine("Запустіть його командою: docker start MySql  (або docker compose --profile all-providers up -d mysql)");
            Console.WriteLine("Та перевірте пароль у appsettings.json -> ConnectionStrings:MySql");
            SqlServerDbHelper.PrintSection("Згенерований MySQL SQL (працює навіть офлайн)");
            Console.WriteLine(db.Users.Where(u => u.Age > 25).ToQueryString());
            return;
        }

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        db.Users.AddRange(
            new User { Name = "Tom", Age = 33 },
            new User { Name = "Alice", Age = 26 });
        db.SaveChanges();

        SqlServerDbHelper.PrintSection("2. Читання даних із MySQL");
        foreach (var u in db.Users.AsNoTracking().ToList())
            Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");

        SqlServerDbHelper.PrintSection("3. Згенерований MySQL SQL (ToQueryString)");
        Console.WriteLine(db.Users.Where(u => u.Age > 25).ToQueryString());
    }
}

using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch02;

/// <summary>
/// Урок 2.3: Провайдер PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL + Docker-контейнер PostgreSQL).
/// </summary>
public class Lesson_2_03_PostgreSqlProvider : ILesson
{
    public string Id => "2.3";
    public string Chapter => "02. Провайдери баз даних";
    public string Title => "Провайдер PostgreSQL";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/7.3.php";
    public int ChapterOrder => 2;
    public int LessonOrder => 3;

    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class PostgresAppContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        public PostgresAppContext(DbContextOptions<PostgresAppContext> options) : base(options) { }
    }

    public void Run()
    {
        var connStr = SqlServerDbHelper.GetConnectionString("PostgreSql");
        var options = new DbContextOptionsBuilder<PostgresAppContext>()
            .UseNpgsql(connStr)
            .Options;

        SqlServerDbHelper.PrintSection("1. Конфігурація провайдера PostgreSQL (UseNpgsql)");
        using var db = new PostgresAppContext(options);
        Console.WriteLine($"Провайдер: {db.Database.ProviderName}");

        if (!db.Database.CanConnect())
        {
            Console.WriteLine("Контейнер PostgreSQL зараз зупинений або недоступний.");
            Console.WriteLine("Запустіть його командою: docker start PostgreSQL  (або docker compose --profile all-providers up -d postgres)");
            Console.WriteLine("Та перевірте пароль у appsettings.json -> ConnectionStrings:PostgreSql");
            SqlServerDbHelper.PrintSection("Згенерований PostgreSQL SQL (працює навіть офлайн)");
            Console.WriteLine(db.Users.Where(u => u.Age > 25).ToQueryString());
            return;
        }

        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        db.Users.AddRange(
            new User { Name = "Tom", Age = 33 },
            new User { Name = "Alice", Age = 26 });
        db.SaveChanges();

        SqlServerDbHelper.PrintSection("2. Читання даних із PostgreSQL");
        foreach (var u in db.Users.AsNoTracking().ToList())
            Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");

        SqlServerDbHelper.PrintSection("3. Згенерований PostgreSQL SQL (ToQueryString)");
        Console.WriteLine(db.Users.Where(u => u.Age > 25).ToQueryString());
    }
}

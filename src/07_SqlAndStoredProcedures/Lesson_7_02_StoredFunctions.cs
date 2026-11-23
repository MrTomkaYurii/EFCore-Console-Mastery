using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch07;

/// <summary>
/// Урок 7.2: Збережені функції MS SQL Server (TVF — Table-Valued Function та скалярна функція через HasDbFunction).
/// </summary>
public class Lesson_7_02_StoredFunctions : ILesson
{
    public string Id => "7.2";
    public string Chapter => "07. SQL в Entity Framework Core";
    public string Title => "Збережені функції (MS SQL Server TVF + HasDbFunction)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/6.2.php";
    public int ChapterOrder => 7;
    public int LessonOrder => 2;

    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class FunctionsContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        // Метод-обгортка для Table-Valued Function [l7_02].[GetUsersByAge](@age)
        public IQueryable<User> GetUsersByAge(int age) => FromExpression(() => GetUsersByAge(age));

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l7_02");
            modelBuilder.HasDbFunction(typeof(FunctionsContext).GetMethod(nameof(GetUsersByAge), [typeof(int)])!)
                .HasSchema("l7_02")
                .HasName("GetUsersByAge");
        }
    }

    public void Run()
    {
        using var db = new FunctionsContext();
        SqlServerDbHelper.RecreateSchema(db, "l7_02");

        // 1. Створюємо реальну табличну функцію (TVF) у MS SQL Server
        db.Database.ExecuteSqlRaw("""
            CREATE FUNCTION [l7_02].[GetUsersByAge] (@age INT)
            RETURNS TABLE
            AS
            RETURN (
                SELECT [Id], [Name], [Age]
                FROM [l7_02].[Users]
                WHERE [Age] > @age
            );
            """);

        db.Users.AddRange(
            new User { Name = "Tom", Age = 36 },
            new User { Name = "Bob", Age = 39 },
            new User { Name = "Alice", Age = 28 },
            new User { Name = "Kate", Age = 25 });
        db.SaveChanges();

        SqlServerDbHelper.PrintSection("1. Виклик табличної функції MS SQL Server через HasDbFunction у LINQ");
        var query = db.GetUsersByAge(30).OrderBy(u => u.Name);
        Console.WriteLine("Згенерований T-SQL:");
        Console.WriteLine(query.ToQueryString());

        Console.WriteLine("\nРезультат:");
        foreach (var u in query.ToList())
            Console.WriteLine($"  {u.Id}. {u.Name} ({u.Age})");

        SqlServerDbHelper.PrintSection("2. Прямий виклик функції через FromSql");
        int threshold = 27;
        var direct = db.Users
            .FromSql($"SELECT * FROM [l7_02].[GetUsersByAge]({threshold})")
            .ToList();
        foreach (var u in direct)
            Console.WriteLine($"  {u.Id}. {u.Name} ({u.Age})");
    }
}

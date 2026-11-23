using EFCoreConsoleMastery.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch07;

/// <summary>
/// Урок 7.1: Виконання сирих SQL-запитів у MS SQL Server
/// (FromSqlRaw, FromSqlInterpolated, Database.SqlQuery, ExecuteSqlRaw / ExecuteSqlInterpolated).
/// </summary>
public class Lesson_7_01_RawSqlQueries : ILesson
{
    public string Id => "7.1";
    public string Chapter => "07. SQL в Entity Framework Core";
    public string Title => "Виконання SQL-запитів (FromSqlRaw, FromSqlInterpolated, SqlQuery, ExecuteSqlRaw)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/6.1.php";
    public int ChapterOrder => 7;
    public int LessonOrder => 1;

    private sealed class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
    }

    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    private sealed class UserSummaryDto
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class RawSqlContext : DbContext
    {
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.HasDefaultSchema("l7_01");
    }

    public void Run()
    {
        using var db = new RawSqlContext();
        SqlServerDbHelper.RecreateSchema(db, "l7_01");

        var ms = new Company { Name = "Microsoft" };
        var google = new Company { Name = "Google" };
        db.Companies.AddRange(ms, google);
        db.Users.AddRange(
            new User { Name = "Tom", Age = 36, Company = ms },
            new User { Name = "Alice", Age = 28, Company = ms },
            new User { Name = "Bob", Age = 39, Company = google });
        db.SaveChanges();

        SqlServerDbHelper.PrintSection("1. FromSqlRaw + композиція з LINQ (OrderBy + Include)");
        var companies = db.Companies
            .FromSqlRaw("SELECT * FROM [l7_01].[Companies]")
            .Include(c => c.Users)
            .OrderBy(c => c.Name)
            .ToList();
        foreach (var c in companies)
            Console.WriteLine($"  {c.Name}: {string.Join(", ", c.Users.Select(u => u.Name))}");

        SqlServerDbHelper.PrintSection("2. Параметризований FromSqlRaw (SqlParameter — захист від SQL Injection)");
        var nameParam = new SqlParameter("@pattern", "%o%");
        var filtered = db.Users
            .FromSqlRaw("SELECT * FROM [l7_01].[Users] WHERE [Name] LIKE @pattern", nameParam)
            .ToList();
        foreach (var u in filtered)
            Console.WriteLine($"  {u.Name} ({u.Age})");

        SqlServerDbHelper.PrintSection("3. FromSql (автоматична параметризація інтерпольованого рядка)");
        int minAge = 30;
        var adults = db.Users
            .FromSql($"SELECT * FROM [l7_01].[Users] WHERE [Age] > {minAge}")
            .ToList();
        foreach (var u in adults)
            Console.WriteLine($"  {u.Name} ({u.Age})");

        SqlServerDbHelper.PrintSection("4. Database.SqlQuery<T> (скаляри та довільні DTO, EF Core 8+)");
        var avgAge = db.Database
            .SqlQuery<int>($"SELECT AVG([Age]) AS [Value] FROM [l7_01].[Users]")
            .First();
        Console.WriteLine($"  Середній вік: {avgAge}");

        var dtos = db.Database
            .SqlQuery<UserSummaryDto>($"SELECT [Name], [Age] FROM [l7_01].[Users] ORDER BY [Age] DESC")
            .ToList();
        foreach (var d in dtos)
            Console.WriteLine($"  DTO: {d.Name} - {d.Age}");

        SqlServerDbHelper.PrintSection("5. ExecuteSql (DML: UPDATE / INSERT / DELETE)");
        int updated = db.Database.ExecuteSql($"UPDATE [l7_01].[Users] SET [Age] = [Age] + 1 WHERE [Age] < {30}");
        Console.WriteLine($"  Оновлено рядків: {updated}");
    }
}

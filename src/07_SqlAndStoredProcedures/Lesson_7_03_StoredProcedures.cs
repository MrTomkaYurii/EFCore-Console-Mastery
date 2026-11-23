using System.Data;
using EFCoreConsoleMastery.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch07;

/// <summary>
/// Урок 7.3: Збережені процедури MS SQL Server (CREATE PROCEDURE, EXEC через FromSqlRaw та OUTPUT-параметри).
/// </summary>
public class Lesson_7_03_StoredProcedures : ILesson
{
    public string Id => "7.3";
    public string Chapter => "07. SQL в Entity Framework Core";
    public string Title => "Збережені процедури (EXEC та OUTPUT-параметри MS SQL Server)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/6.3.php";
    public int ChapterOrder => 7;
    public int LessonOrder => 3;

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

    private sealed class ProceduresContext : DbContext
    {
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.HasDefaultSchema("l7_03");
    }

    public void Run()
    {
        using var db = new ProceduresContext();
        SqlServerDbHelper.RecreateSchema(db, "l7_03");

        // Створюємо 2 реальні збережені процедури в MS SQL Server
        db.Database.ExecuteSqlRaw("""
            CREATE PROCEDURE [l7_03].[GetUsersByCompany]
                @name NVARCHAR(100)
            AS
            BEGIN
                SELECT u.[Id], u.[Name], u.[Age], u.[CompanyId]
                FROM [l7_03].[Users] u
                INNER JOIN [l7_03].[Companies] c ON u.[CompanyId] = c.[Id]
                WHERE c.[Name] = @name;
            END
            """);

        db.Database.ExecuteSqlRaw("""
            CREATE PROCEDURE [l7_03].[GetUserWithMaxAge]
                @name NVARCHAR(100) OUTPUT
            AS
            BEGIN
                SELECT TOP(1) @name = [Name]
                FROM [l7_03].[Users]
                ORDER BY [Age] DESC;
            END
            """);

        var ms = new Company { Name = "Microsoft" };
        var google = new Company { Name = "Google" };
        db.Companies.AddRange(ms, google);
        db.Users.AddRange(
            new User { Name = "Tom", Age = 36, Company = ms },
            new User { Name = "Alice", Age = 28, Company = ms },
            new User { Name = "Bob", Age = 42, Company = google });
        db.SaveChanges();

        SqlServerDbHelper.PrintSection("1. Виклик процедури GetUsersByCompany через FromSqlRaw");
        var companyParam = new SqlParameter("@name", "Microsoft");
        var users = db.Users
            .FromSqlRaw("EXEC [l7_03].[GetUsersByCompany] @name", companyParam)
            .ToList();
        foreach (var u in users)
            Console.WriteLine($"  {u.Id}. {u.Name} ({u.Age})");

        SqlServerDbHelper.PrintSection("2. Виклик процедури з OUTPUT-параметром (GetUserWithMaxAge)");
        var outParam = new SqlParameter
        {
            ParameterName = "@name",
            SqlDbType = SqlDbType.NVarChar,
            Size = 100,
            Direction = ParameterDirection.Output
        };
        db.Database.ExecuteSqlRaw("EXEC [l7_03].[GetUserWithMaxAge] @name OUT", outParam);
        Console.WriteLine($"  Найстарший користувач (OUTPUT): {outParam.Value}");
    }
}

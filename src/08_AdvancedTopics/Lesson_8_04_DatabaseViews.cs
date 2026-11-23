using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch08;

/// <summary>
/// Урок 8.4: Проєкція запитів на представлення БД (MS SQL Server CREATE VIEW + HasNoKey().ToView()).
/// </summary>
public class Lesson_8_04_DatabaseViews : ILesson
{
    public string Id => "8.4";
    public string Chapter => "08. Додаткові теми";
    public string Title => "Проєкція запитів на представлення (Views / ToView)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/8.2.php";
    public int ChapterOrder => 8;
    public int LessonOrder => 4;

    private sealed class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<Product> Products { get; set; } = new();
    }

    private sealed class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    // Keyless-сутність для представлення View_ProductsByCompany
    private sealed class ProductByCompanyView
    {
        public string? ProductName { get; set; }
        public decimal Price { get; set; }
        public string? CompanyName { get; set; }
    }

    private sealed class ViewsContext : DbContext
    {
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductByCompanyView> ProductsByCompany => Set<ProductByCompanyView>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l8_04");
            modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<ProductByCompanyView>()
                .HasNoKey()
                .ToView("View_ProductsByCompany", "l8_04");
        }
    }

    public void Run()
    {
        using var db = new ViewsContext();
        SqlServerDbHelper.RecreateSchema(db, "l8_04");

        db.Database.ExecuteSqlRaw("""
            CREATE VIEW [l8_04].[View_ProductsByCompany] AS
            SELECT p.[Name] AS [ProductName], p.[Price], c.[Name] AS [CompanyName]
            FROM [l8_04].[Products] p
            INNER JOIN [l8_04].[Companies] c ON p.[CompanyId] = c.[Id];
            """);

        var ms = new Company { Name = "Microsoft" };
        var apple = new Company { Name = "Apple" };
        db.Companies.AddRange(ms, apple);
        db.Products.AddRange(
            new Product { Name = "Surface", Price = 1200m, Company = ms },
            new Product { Name = "Xbox", Price = 500m, Company = ms },
            new Product { Name = "MacBook", Price = 2100m, Company = apple });
        db.SaveChanges();

        SqlServerDbHelper.PrintSection("1. Запит до представлення [l8_04].[View_ProductsByCompany]");
        using var readDb = new ViewsContext();
        foreach (var row in readDb.ProductsByCompany.ToList())
            Console.WriteLine($"  {row.CompanyName}: {row.ProductName} — ${row.Price}");

        SqlServerDbHelper.PrintSection("2. LINQ-фільтрація поверх View + перевірка ChangeTracker");
        var expensive = readDb.ProductsByCompany
            .Where(v => v.Price >= 1000m)
            .OrderByDescending(v => v.Price)
            .ToList();
        foreach (var row in expensive)
            Console.WriteLine($"  {row.ProductName} (${row.Price})");

        Console.WriteLine($"  Записів у ChangeTracker (Keyless не відстежуються): {readDb.ChangeTracker.Entries().Count()}");
    }
}

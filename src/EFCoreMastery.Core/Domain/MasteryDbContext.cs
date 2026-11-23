using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EFCoreConsoleMastery.Core.Domain;

/// <summary>Країна розташування компанії (таблиця dbo.Countries).</summary>
public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Company> Companies { get; set; } = new();
}

/// <summary>Компанія (таблиця dbo.Companies).</summary>
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? CountryId { get; set; }
    public Country? Country { get; set; }
    public List<User> Users { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}

/// <summary>Роль користувача (таблиця dbo.Roles).</summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<User> Users { get; set; } = new();
}

/// <summary>Користувач (таблиця dbo.Users).</summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public int? RoleId { get; set; }
    public Role? Role { get; set; }
    public UserProfile? Profile { get; set; }
}

/// <summary>Профіль користувача 1:1 (таблиця dbo.UserProfiles).</summary>
public class UserProfile
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
}

/// <summary>Продукт компанії (таблиця dbo.Products).</summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
}

/// <summary>Навчальний курс для зв'язку M:N (таблиця dbo.Courses).</summary>
public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Student> Students { get; set; } = new();
}

/// <summary>Студент для зв'язку M:N (таблиця dbo.Students).</summary>
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Course> Courses { get; set; } = new();
}

/// <summary>
/// Наскрізний доменний DbContext курсу (схема dbo в EFCoreMasteryDb).
/// Об'єднує 8 зв'язаних таблиць (Countries, Companies, Roles, Users, UserProfiles, Products, Courses, Students)
/// та підтримує реальні EF Core Migrations через MasteryDbContextFactory.
/// </summary>
public class MasteryDbContext : DbContext
{
    private readonly string? _connectionString;

    public MasteryDbContext()
    {
    }

    public MasteryDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public MasteryDbContext(DbContextOptions<MasteryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(_connectionString ?? SqlServerDbHelper.ConnectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId);
    }

    /// <summary>
    /// Перестворює схему dbo та наповнює її базовим набором тестових даних
    /// для уроків CRUD, LINQ, Joins, Views та Stored Procedures.
    /// </summary>
    public static void ResetAndSeed()
    {
        using var db = new MasteryDbContext();
        SqlServerDbHelper.RecreateSchema(db, "dbo");

        var usa = new Country { Name = "USA" };
        var ukraine = new Country { Name = "Ukraine" };

        var microsoft = new Company { Name = "Microsoft", Country = usa };
        var google = new Company { Name = "Google", Country = usa };
        var softserve = new Company { Name = "SoftServe", Country = ukraine };

        var adminRole = new Role { Name = "Admin" };
        var userRole = new Role { Name = "User" };

        var tom = new User { Name = "Tom", Age = 36, Company = microsoft, Role = adminRole };
        var bob = new User { Name = "Bob", Age = 39, Company = google, Role = userRole };
        var alice = new User { Name = "Alice", Age = 28, Company = microsoft, Role = userRole };
        var kate = new User { Name = "Kate", Age = 25, Company = softserve, Role = adminRole };

        var p1 = new Product { Name = "Visual Studio", Price = 499.00m, Company = microsoft };
        var p2 = new Product { Name = "Azure", Price = 199.00m, Company = microsoft };
        var p3 = new Product { Name = "Pixel", Price = 799.00m, Company = google };

        var efCourse = new Course { Name = "EF Core Mastery" };
        var sqlCourse = new Course { Name = "T-SQL Fundamentals" };
        var s1 = new Student { Name = "Andrii", Courses = { efCourse, sqlCourse } };
        var s2 = new Student { Name = "Olena", Courses = { efCourse } };

        db.Countries.AddRange(usa, ukraine);
        db.Companies.AddRange(microsoft, google, softserve);
        db.Roles.AddRange(adminRole, userRole);
        db.Users.AddRange(tom, bob, alice, kate);
        db.Products.AddRange(p1, p2, p3);
        db.Students.AddRange(s1, s2);
        db.SaveChanges();
    }
}

/// <summary>
/// Фабрика для CLI-інструментів EF Core (dotnet ef migrations add / database update).
/// </summary>
public class MasteryDbContextFactory : IDesignTimeDbContextFactory<MasteryDbContext>
{
    public MasteryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MasteryDbContext>();
        optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        return new MasteryDbContext(optionsBuilder.Options);
    }
}

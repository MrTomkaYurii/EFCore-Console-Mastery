using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.13: Конфігурація моделей (IEntityTypeConfiguration).
/// Демонструє винесення Fluent API конфігурації з OnModelCreating у окремі класи
/// через ApplyConfiguration(), методи-делегати та атрибут [EntityTypeConfiguration].
/// </summary>
public class Lesson_2_13_ModelConfiguration : ILesson
{
    public string Id => "2.13";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Конфігурація моделей (IEntityTypeConfiguration)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.13.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 12;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.13. Конфігурація моделей (IEntityTypeConfiguration)");

        // Варіант 1: modelBuilder.ApplyConfiguration(...)
        using (var db = new L2_13.ApplyConfigContext())
        {
            SqliteDbHelper.RecreateDatabase(db);
            db.Companies.Add(new L2_13.Company { Name = "SoftServe" });
            db.Users.Add(new L2_13.User { Name = "Tom", Age = 29 });
            db.SaveChanges();

            var tables = db.Database
                .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
                .ToList();

            Console.WriteLine("1) ApplyConfigContext (ApplyConfiguration) — створені таблиці:");
            foreach (var t in tables) Console.WriteLine($"  - {t}");
        }

        // Варіант 2: Атрибут [EntityTypeConfiguration(typeof(...))]
        using (var db = new L2_13.AttributeConfigContext())
        {
            SqliteDbHelper.RecreateDatabase(db);
            db.Orders.Add(new L2_13.Order { Number = "ORD-2026-01" });
            db.SaveChanges();

            var orderSql = db.Database
                .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='CustomerOrders'")
                .FirstOrDefault();

            Console.WriteLine("\n2) AttributeConfigContext ([EntityTypeConfiguration]) — DDL таблиці:");
            Console.WriteLine(orderSql);
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_13
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("People");
            builder.Property(p => p.Id).HasColumnName("user_id");
            builder.Property(p => p.Name).IsRequired();
        }
    }

    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Enterprises");
            builder.Property(c => c.Name).IsRequired();
        }
    }

    public class ApplyConfigContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Company> Companies => Set<Company>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_13_apply"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Підключення окремих класів конфігурації
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyConfiguration());

            // Примітка: у реальних проєктах також можна викликати
            // modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplyConfigContext).Assembly);
        }
    }

    // Демонстрація атрибута [EntityTypeConfiguration] (EF Core 7+)
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("CustomerOrders");
            builder.Property(o => o.Number).IsRequired().HasMaxLength(32);
        }
    }

    [EntityTypeConfiguration(typeof(OrderConfiguration))]
    public class Order
    {
        public int Id { get; set; }
        public string? Number { get; set; }
    }

    public class AttributeConfigContext : DbContext
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_13_attr"));
        }
    }
}

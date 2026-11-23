using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 3.12: Конфігурація моделей (IEntityTypeConfiguration).
/// Демонструє винесення Fluent API конфігурації з OnModelCreating у окремі класи
/// через ApplyConfiguration(), методи-делегати та атрибут [EntityTypeConfiguration].
/// </summary>
public class Lesson_3_12_ModelConfiguration : ILesson
{
    public string Id => "3.12";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Конфігурація моделей (IEntityTypeConfiguration)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.13.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 12;

    /// <summary>
    /// Запускає демонстрацію двох способів винесення конфігурації сутностей із OnModelCreating:
    /// 1) через IEntityTypeConfiguration&lt;T&gt; + modelBuilder.ApplyConfiguration(...);
    /// 2) через атрибут [EntityTypeConfiguration(typeof(...))] без перевизначення OnModelCreating.
    /// </summary>
    public void Run()
    {
        SqlServerDbHelper.PrintSection("3.12. Конфігурація моделей (IEntityTypeConfiguration)");

        // Варіант 1: modelBuilder.ApplyConfiguration(...)
        using (var db = new L2_13.ApplyConfigContext())
        {
            SqlServerDbHelper.RecreateSchema(db, "l3_12");
            db.Companies.Add(new L2_13.Company { Name = "SoftServe" });
            db.Users.Add(new L2_13.User { Name = "Tom", Age = 29 });
            db.SaveChanges();

            // Перевіряємо, що таблиці створені з новими іменами ("People" та "Enterprises"), заданими у конфігураціях.
            var tables = db.Database.GenerateCreateScript();

            Console.WriteLine("1) ApplyConfigContext (ApplyConfiguration) — створені таблиці:");
            foreach (var t in tables) Console.WriteLine($"  - {t}");
        }

        // Варіант 2: Атрибут [EntityTypeConfiguration(typeof(...))]
        using (var db = new L2_13.AttributeConfigContext())
        {
            SqlServerDbHelper.RecreateSchema(db, "l3_12");
            db.Orders.Add(new L2_13.Order { Number = "ORD-2026-01" });
            db.SaveChanges();

            var orderSql = db.Database.GenerateCreateScript();

            Console.WriteLine("\n2) AttributeConfigContext ([EntityTypeConfiguration]) — DDL таблиці:");
            Console.WriteLine(orderSql);
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_13
{
    /// <summary>Сутність користувача, конфігурація якої винесена в <see cref="UserConfiguration"/>.</summary>
    public class User
    {
        /// <summary>Первинний ключ (у БД буде перейменований на "user_id").</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача (у БД стане NOT NULL через IsRequired()).</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }
    }

    /// <summary>Сутність компанії, конфігурація якої винесена в <see cref="CompanyConfiguration"/>.</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії (у БД стане NOT NULL через IsRequired()).</summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Ізольований клас конфігурації Fluent API для сутності <see cref="User"/>.
    /// Дозволяє не перевантажувати метод OnModelCreating у великих проєктах.
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        /// <summary>Застосовує правила відображення для сутності User.</summary>
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("People");
            builder.Property(p => p.Id).HasColumnName("user_id");
            builder.Property(p => p.Name).IsRequired();
        }
    }

    /// <summary>
    /// Ізольований клас конфігурації Fluent API для сутності <see cref="Company"/>.
    /// </summary>
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        /// <summary>Застосовує правила відображення для сутності Company.</summary>
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Enterprises");
            builder.Property(c => c.Name).IsRequired();
        }
    }

    /// <summary>
    /// Контекст, що підключає конфігурації явно через modelBuilder.ApplyConfiguration(...).
    /// </summary>
    public class ApplyConfigContext : DbContext
    {
        /// <summary>Набір користувачів (таблиця "People").</summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>Набір компаній (таблиця "Enterprises").</summary>
        public DbSet<Company> Companies => Set<Company>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l3_12");
            // Підключення окремих класів конфігурації вручну
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyConfiguration());

            // Примітка: у реальних проєктах із десятками сутностей зручніше викликати
            // modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplyConfigContext).Assembly),
            // щоб EF Core автоматично знайшов усі реалізації IEntityTypeConfiguration<T> у збірці.
        }
    }

    /// <summary>
    /// Конфігурація для сутності <see cref="Order"/>, що підключається автоматично через атрибут [EntityTypeConfiguration].
    /// </summary>
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        /// <summary>Застосовує правила відображення для сутності Order.</summary>
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("CustomerOrders");
            // HasMaxLength(32) у SQLite не обрізає TEXT, але враховується у моделі та в SQL Server/PostgreSQL.
            builder.Property(o => o.Number).IsRequired().HasMaxLength(32);
        }
    }

    /// <summary>
    /// Сутність замовлення, пов'язана зі своїм класом конфігурації через атрибут
    /// [EntityTypeConfiguration(typeof(OrderConfiguration))] (доступний починаючи з EF Core 7).
    /// Завдяки цьому атрибуту в DbContext взагалі не потрібно перевизначати OnModelCreating.
    /// </summary>
    [EntityTypeConfiguration(typeof(OrderConfiguration))]
    public class Order
    {
        /// <summary>Первинний ключ замовлення.</summary>
        public int Id { get; set; }

        /// <summary>Номер замовлення (обов'язковий стовпець NOT NULL).</summary>
        public string? Number { get; set; }
    }

    /// <summary>
    /// Контекст, який не перевизначає OnModelCreating: EF Core знаходить OrderConfiguration
    /// автоматично завдяки атрибуту [EntityTypeConfiguration] на класі Order.
    /// </summary>
    public class AttributeConfigContext : DbContext
    {
        /// <summary>Набір замовлень (таблиця "CustomerOrders").</summary>
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }
    }
}

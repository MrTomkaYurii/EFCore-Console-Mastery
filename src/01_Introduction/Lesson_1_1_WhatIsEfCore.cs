using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 1.1. Що таке Entity Framework Core.
/// Демонстрація базового DbContext, сутності та інспекції метаданих моделі EF Core у SQLite.
/// </summary>
public class Lesson_1_1_WhatIsEfCore : ILesson
{
    public string Id => "1.1";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Що таке Entity Framework Core";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.1.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 1;

    public void Run()
    {
        var connectionString = SqliteDbHelper.GetConnectionString("lesson_1_1");

        using var db = new L1_1.IntroContext(connectionString);
        SqliteDbHelper.RecreateDatabase(db);

        // 1. Інформація про провайдер БД
        SqliteDbHelper.PrintSection("1. Провайдер бази даних");
        Console.WriteLine($"Поточний провайдер: {db.Database.ProviderName}");
        Console.WriteLine($"Рядок підключення:  {connectionString}");

        // 2. Інспекція метаданих моделі EF Core (як C#-класи відображаються на таблиці SQLite)
        SqliteDbHelper.PrintSection("2. Інспекція метаданих моделі (IModel)");
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            Console.WriteLine($"Сутність (CLR-тип): {entityType.ClrType.Name}");
            Console.WriteLine($"  Таблиця в БД: {entityType.GetTableName()}");

            var pk = entityType.FindPrimaryKey();
            var pkNames = pk is null ? "немає" : string.Join(", ", pk.Properties.Select(p => p.Name));
            Console.WriteLine($"  Первинний ключ: {pkNames}");

            Console.WriteLine("  Властивості -> Стовпці:");
            foreach (var prop in entityType.GetProperties())
            {
                Console.WriteLine(
                    $"    - {prop.Name} ({prop.ClrType.Name}) -> стовпець \"{prop.GetColumnName()}\", тип SQLite: {prop.GetColumnType()}, Nullable: {prop.IsNullable}");
            }
        }

        // 3. Робота з об'єктами замість ручного SQL (ORM + LINQ)
        SqliteDbHelper.PrintSection("3. Додавання та LINQ-вибірка сутностей");
        db.Products.AddRange(
            new L1_1.Product { Title = "Ноутбук", Price = 32500m },
            new L1_1.Product { Title = "Клавіатура", Price = 1850m },
            new L1_1.Product { Title = "Монітор", Price = 11200m }
        );
        db.SaveChanges();

        // EF Core автоматично транслює цей LINQ-запит у SQL SELECT ... WHERE ... ORDER BY ...
        var expensiveProducts = db.Products
            .Where(p => p.Price > 2000m)
            .OrderByDescending(p => p.Price)
            .ToList();

        Console.WriteLine("Товари дорожчі за 2000 грн:");
        foreach (var item in expensiveProducts)
        {
            Console.WriteLine($"  [{item.Id}] {item.Title} — {item.Price} грн");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch01.L1_1
{
    /// <summary>
    /// Сутність (Entity), що представляє товар у каталозі.
    /// EF Core автоматично зіставляє властивість Id з первинним ключем таблиці.
    /// </summary>
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    /// <summary>
    /// Контекст бази даних — головний міст між C#-об'єктами та реляційною БД.
    /// </summary>
    public class IntroContext(string connectionString) : DbContext
    {
        public DbSet<Product> Products => Set<Product>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}

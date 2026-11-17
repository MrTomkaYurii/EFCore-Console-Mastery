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

    /// <summary>
    /// Запускає демонстрацію уроку: підключення до SQLite, інспекцію метаданих моделі EF Core
    /// та виконання базового LINQ-запиту замість ручного написання SQL.
    /// </summary>
    public void Run()
    {
        // Формуємо рядок підключення до локального файлу SQLite (Data Source=...)
        var connectionString = SqliteDbHelper.GetConnectionString("lesson_1_1");

        // Використовуємо using var, щоб DbContext гарантовано звільнив з'єднання з БД та Change Tracker
        using var db = new L1_1.IntroContext(connectionString);
        // Перестворюємо БД (EnsureDeleted + EnsureCreated), щоб кожен запуск починався з чистої схеми
        SqliteDbHelper.RecreateDatabase(db);

        // 1. Інформація про провайдер БД
        SqliteDbHelper.PrintSection("1. Провайдер бази даних");
        // ProviderName показує активний NuGet-провайдер (Microsoft.EntityFrameworkCore.Sqlite),
        // який відповідає за діалект SQL, типи стовпців та трансляцію LINQ у SQL
        Console.WriteLine($"Поточний провайдер: {db.Database.ProviderName}");
        Console.WriteLine($"Рядок підключення:  {connectionString}");

        // 2. Інспекція метаданих моделі EF Core (як C#-класи відображаються на таблиці SQLite)
        // EF Core будує внутрішню модель IModel один раз при першому зверненні до контексту
        SqliteDbHelper.PrintSection("2. Інспекція метаданих моделі (IModel)");
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            Console.WriteLine($"Сутність (CLR-тип): {entityType.ClrType.Name}");
            // За конвенцією назва таблиці береться з імені властивості DbSet<T> у DbContext ("Products")
            Console.WriteLine($"  Таблиця в БД: {entityType.GetTableName()}");

            var pk = entityType.FindPrimaryKey();
            var pkNames = pk is null ? "немає" : string.Join(", ", pk.Properties.Select(p => p.Name));
            Console.WriteLine($"  Первинний ключ: {pkNames}");

            Console.WriteLine("  Властивості -> Стовпці:");
            foreach (var prop in entityType.GetProperties())
            {
                // Зверніть увагу на відмінність SQLite від MS SQL Server:
                // - int -> INTEGER (у SQL Server: int)
                // - string (non-nullable) -> TEXT NOT NULL (у SQL Server: nvarchar(max) NOT NULL)
                // - decimal -> TEXT (у SQLite немає точного типу DECIMAL/NUMERIC, тому EF Core зберігає decimal як TEXT,
                //   щоб не втрачати точність; у SQL Server це був би decimal(18,2))
                Console.WriteLine(
                    $"    - {prop.Name} ({prop.ClrType.Name}) -> стовпець \"{prop.GetColumnName()}\", тип SQLite: {prop.GetColumnType()}, Nullable: {prop.IsNullable}");
            }
        }

        // 3. Робота з об'єктами замість ручного SQL (ORM + LINQ)
        SqliteDbHelper.PrintSection("3. Додавання та LINQ-вибірка сутностей");
        // AddRange переводить усі передані об'єкти у стан EntityState.Added у Change Tracker.
        // На цьому етапі SQL-запити до БД ще НЕ надсилаються.
        db.Products.AddRange(
            new L1_1.Product { Title = "Ноутбук", Price = 32500m },
            new L1_1.Product { Title = "Клавіатура", Price = 1850m },
            new L1_1.Product { Title = "Монітор", Price = 11200m }
        );
        // SaveChanges() відкриває транзакцію, генерує INSERT INTO "Products" ... RETURNING "Id"
        // та заповнює згенеровані SQLite значення Id назад у C#-об'єкти
        db.SaveChanges();

        // EF Core автоматично транслює цей LINQ-запит у SQL SELECT ... WHERE ... ORDER BY ...
        // Матеріалізація відбувається лише під час виклику .ToList() (відкладене виконання / deferred execution).
        // Важливий нюанс SQLite: оскільки decimal зберігається як TEXT, порівняння та сортування за Price
        // може вимагати обережності (у складних виразів EF Core 8+ застосовує ef_compare або клієнтську оцінку).
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
        /// <summary>
        /// Первинний ключ таблиці Products. За конвенцією властивість з іменем Id
        /// отримує INTEGER PRIMARY KEY AUTOINCREMENT у SQLite (аналог IDENTITY(1,1) у SQL Server).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Назва товару. Оскільки увімкнено nullable reference types і тип string (без ?),
        /// EF Core створює стовпець TEXT NOT NULL у SQLite.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Ціна товару. У SQLite відображається на тип TEXT для збереження десяткової точності.
        /// </summary>
        public decimal Price { get; set; }
    }

    /// <summary>
    /// Контекст бази даних — головний міст між C#-об'єктами та реляційною БД.
    /// Керує підключенням, метаданими моделі, Change Tracker (відстеженням змін) і транзакціями.
    /// </summary>
    public class IntroContext(string connectionString) : DbContext
    {
        /// <summary>
        /// Набір сутностей Product, що відповідає таблиці "Products" у базі даних.
        /// Використання => Set&lt;Product&gt;() є рекомендованим способом без CS8618.
        /// </summary>
        public DbSet<Product> Products => Set<Product>();

        /// <summary>
        /// Налаштовує провайдер бази даних та параметри підключення для цього контексту.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Підключаємо провайдер SQLite; для MS SQL Server тут був би виклик UseSqlServer(...)
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}

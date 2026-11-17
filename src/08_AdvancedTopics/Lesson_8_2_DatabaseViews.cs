using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch08.L8_2;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch08
{
    /// <summary>
    /// Урок 8.2: Проєкція запитів на представлення БД (Views / <c>ToView</c> + <c>HasNoKey</c>).
    /// </summary>
    public class Lesson_8_2_DatabaseViews : ILesson
    {
        public string Id => "8.2";
        public string Chapter => "08. Додаткові теми";
        public string Title => "Проєкція запитів на представлення (Views / ToView)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/8.2.php";
        public int ChapterOrder => 8;
        public int LessonOrder => 4;

        public void Run()
        {
            var connStr = SqliteDbHelper.GetConnectionString("lesson_8_2_views");

            // 1. Створення таблиць, представлення (VIEW) та наповнення тестовими даними
            using (var setupDb = new ViewsContext(connStr))
            {
                SqliteDbHelper.RecreateDatabase(setupDb);

                // EnsureCreated() створює таблиці, але не створює SQL VIEW автоматично,
                // тому створюємо представлення у SQLite вручну через ExecuteSqlRaw
                setupDb.Database.ExecuteSqlRaw("""
                    CREATE VIEW IF NOT EXISTS View_ProductsByCompany AS
                    SELECT
                        c.Name AS CompanyName,
                        COUNT(p.Id) AS ProductCount,
                        SUM(p.Price * p.TotalCount) AS TotalSum
                    FROM Companies c
                    INNER JOIN Products p ON p.CompanyId = c.Id
                    GROUP BY c.Name;
                    """);

                var apple = new Company { Name = "Apple" };
                var samsung = new Company { Name = "Samsung" };
                var huawei = new Company { Name = "Huawei" };
                setupDb.Companies.AddRange(apple, samsung, huawei);

                setupDb.Products.AddRange(
                    new Product { Name = "iPhone 15", Company = apple, Price = 40000, TotalCount = 2 },
                    new Product { Name = "iPhone 14", Company = apple, Price = 30000, TotalCount = 4 },
                    new Product { Name = "Galaxy S24", Company = samsung, Price = 35000, TotalCount = 3 },
                    new Product { Name = "Galaxy A55", Company = samsung, Price = 15000, TotalCount = 5 },
                    new Product { Name = "Mate 60", Company = huawei, Price = 20000, TotalCount = 7 }
                );
                setupDb.SaveChanges();
            }

            // -------------------------------------------------------------------------
            // 2. Вибірка всіх даних із представлення View_ProductsByCompany
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("1. Запит до представлення View_ProductsByCompany");

            using (var db = new ViewsContext(connStr))
            {
                var companyProducts = db.ProductsByCompany.ToList();
                foreach (var item in companyProducts)
                {
                    Console.WriteLine(
                        $"Компанія: {item.CompanyName,-10} | Товарів: {item.ProductCount} | Загальна сума: {item.TotalSum:N0} грн");
                }
            }

            // -------------------------------------------------------------------------
            // 3. LINQ-фільтрація та сортування поверх представлення (без відстеження)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("2. LINQ-фільтрація поверх DbSet представлення + перевірка ChangeTracker");

            using (var db = new ViewsContext(connStr))
            {
                var topCompanies = db.ProductsByCompany
                    .Where(g => g.TotalSum > 150_000)
                    .OrderByDescending(g => g.TotalSum)
                    .ToList();

                Console.WriteLine("Компанії із сумою товарів > 150 000 грн:");
                foreach (var item in topCompanies)
                {
                    Console.WriteLine($"  -> {item.CompanyName}: {item.TotalSum:N0} грн");
                }

                // Сутності без ключа (HasNoKey) ніколи не відстежуються ChangeTracker
                int trackedCount = db.ChangeTracker.Entries().Count();
                Console.WriteLine($"\nКількість відстежуваних сутностей у ChangeTracker: {trackedCount} (тільки для читання)");
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch08.L8_2
{
    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<Product> Products { get; set; } = new();
    }

    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Price { get; set; }
        public int TotalCount { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Клас-проєкція (без первинного ключа), зіставлений з SQL-представленням <c>View_ProductsByCompany</c>.
    /// </summary>
    public class CompanyProductsGroup
    {
        public string? CompanyName { get; set; }
        public int ProductCount { get; set; }
        public int TotalSum { get; set; }
    }

    public class ViewsContext : DbContext
    {
        private readonly string _connectionString;

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>
        /// Набір даних, зіставлений із представленням БД (тільки для читання).
        /// </summary>
        public DbSet<CompanyProductsGroup> ProductsByCompany { get; set; } = null!;

        public ViewsContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompanyProductsGroup>(pc =>
            {
                // Представлення зазвичай не мають первинного ключа
                pc.HasNoKey();
                // Вказуємо EF Core, що це VIEW (EnsureCreated не створюватиме для нього таблицю)
                pc.ToView("View_ProductsByCompany");
            });
        }
    }
}

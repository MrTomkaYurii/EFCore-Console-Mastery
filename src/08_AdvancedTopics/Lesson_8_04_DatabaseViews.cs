using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch08.L8_2;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch08
{
    /// <summary>
    /// Урок 8.4: Проєкція запитів на представлення БД (Views / <c>ToView</c> + <c>HasNoKey</c>).
    /// </summary>
    public class Lesson_8_04_DatabaseViews : ILesson
    {
        public string Id => "8.4";
        public string Chapter => "08. Додаткові теми";
        public string Title => "Проєкція запитів на представлення (Views / ToView)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/8.2.php";
        public int ChapterOrder => 8;
        public int LessonOrder => 4;

        /// <summary>
        /// Запускає демонстрацію роботи з представленнями БД (SQL Views) у EF Core:
        /// 1) Ручне створення <c>VIEW</c> у SQLite через <c>ExecuteSqlRaw</c> (оскільки <c>EnsureCreated()</c> не створює VIEW).
        /// 2) Вибірка агрегованих даних через <see cref="ViewsContext.ProductsByCompany"/>.
        /// 3) Серверна LINQ-фільтрація поверх представлення та перевірка відсутності відстеження у <c>ChangeTracker</c>.
        /// </summary>
        public void Run()
        {
            var connStr = SqliteDbHelper.GetConnectionString("lesson_8_2_views");

            // 1. Створення таблиць, представлення (VIEW) та наповнення тестовими даними
            using (var setupDb = new ViewsContext(connStr))
            {
                SqliteDbHelper.RecreateDatabase(setupDb);

                // ВАЖЛИВО: EnsureCreated() (і міграції за замовчуванням) створює лише таблиці,
                // але НЕ створює SQL VIEW автоматично, навіть якщо в OnModelCreating вказано ToView(...).
                // У реальних проєктах CREATE VIEW додають у файл міграції через migrationBuilder.Sql(...),
                // а тут створюємо представлення в SQLite вручну через ExecuteSqlRaw.
                // Примітка: у SQLite представлення завжди віртуальні (обчислюються при кожному SELECT),
                // на відміну від SQL Server, де існують індексовані (матеріалізовані) представлення.
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
                // EF Core генерує звичайний SELECT до представлення:
                // SELECT "v"."CompanyName", "v"."ProductCount", "v"."TotalSum" FROM "View_ProductsByCompany" AS "v"
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
                // Уся фільтрація Where та сортування OrderByDescending транслюються в SQL
                // і виконуються на стороні SQLite (WHERE "v"."TotalSum" > 150000 ORDER BY "v"."TotalSum" DESC)
                var topCompanies = db.ProductsByCompany
                    .Where(g => g.TotalSum > 150_000)
                    .OrderByDescending(g => g.TotalSum)
                    .ToList();

                Console.WriteLine("Компанії із сумою товарів > 150 000 грн:");
                foreach (var item in topCompanies)
                {
                    Console.WriteLine($"  -> {item.CompanyName}: {item.TotalSum:N0} грн");
                }

                // Сутності без ключа (HasNoKey) автоматично працюють у режимі NoTracking:
                // вони ніколи не потрапляють до ChangeTracker, і їх не можна змінювати через SaveChanges().
                int trackedCount = db.ChangeTracker.Entries().Count();
                Console.WriteLine($"\nКількість відстежуваних сутностей у ChangeTracker: {trackedCount} (тільки для читання)");
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch08.L8_2
{
    /// <summary>
    /// Модель компанії-виробника (головна сутність у зв'язку 1:N із <see cref="Product"/>).
    /// </summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Навігаційна властивість: колекція товарів цієї компанії.</summary>
        public List<Product> Products { get; set; } = new();
    }

    /// <summary>
    /// Модель товару на складі (залежна сутність із зовнішнім ключем <see cref="CompanyId"/>).
    /// </summary>
    public class Product
    {
        /// <summary>Первинний ключ товару.</summary>
        public int Id { get; set; }

        /// <summary>Назва товару.</summary>
        public string? Name { get; set; }

        /// <summary>Ціна за одиницю товару.</summary>
        public int Price { get; set; }

        /// <summary>Кількість одиниць на складі.</summary>
        public int TotalCount { get; set; }

        /// <summary>Зовнішній ключ до таблиці <c>Companies</c>.</summary>
        public int CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії-виробника.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Клас-проєкція (Keyless Entity Type — без первинного ключа), зіставлений з SQL-представленням
    /// <c>View_ProductsByCompany</c>. Назви властивостей збігаються з псевдонімами стовпців у <c>SELECT</c>.
    /// </summary>
    public class CompanyProductsGroup
    {
        /// <summary>Назва компанії (<c>c.Name AS CompanyName</c>).</summary>
        public string? CompanyName { get; set; }

        /// <summary>Кількість позицій товарів компанії (<c>COUNT(p.Id) AS ProductCount</c>).</summary>
        public int ProductCount { get; set; }

        /// <summary>Загальна вартість товарів компанії на складі (<c>SUM(p.Price * p.TotalCount) AS TotalSum</c>).</summary>
        public int TotalSum { get; set; }
    }

    /// <summary>
    /// Контекст даних уроку 8.2, що містить звичайні таблиці та набір даних, зіставлений із SQL VIEW.
    /// </summary>
    public class ViewsContext : DbContext
    {
        private readonly string _connectionString;

        /// <summary>Таблиця товарів.</summary>
        public DbSet<Product> Products { get; set; } = null!;

        /// <summary>Таблиця компаній.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>
        /// Набір даних, зіставлений із представленням БД (тільки для читання, без відстеження).
        /// </summary>
        public DbSet<CompanyProductsGroup> ProductsByCompany { get; set; } = null!;

        /// <summary>
        /// Ініціалізує контекст із рядком підключення до SQLite.
        /// </summary>
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
                // Представлення зазвичай не мають первинного ключа, тому обов'язково викликаємо HasNoKey().
                // Це вимикає ChangeTracker для даної сутності та забороняє операції INSERT/UPDATE/DELETE.
                pc.HasNoKey();

                // Вказуємо EF Core, що це SQL VIEW, а не таблиця:
                // 1) EnsureCreated() і міграції не створюватимуть таблицю з таким іменем.
                // 2) Запити до DbSet<CompanyProductsGroup> транслюватимуться у SELECT ... FROM "View_ProductsByCompany".
                pc.ToView("View_ProductsByCompany");
            });
        }
    }
}

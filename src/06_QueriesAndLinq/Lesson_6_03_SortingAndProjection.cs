using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_10;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 6.3: Сортування та проекція з бази даних.
/// Демонструє Select (в анонімний тип і DTO-клас), OrderBy, OrderByDescending, ThenBy, ThenByDescending.
/// </summary>
public class Lesson_6_03_SortingAndProjection : ILesson
{
    public string Id => "6.3";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Сортування та проекція з бази даних";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.10.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 3;

    /// <summary>
    /// Запускає демонстрацію проекцій (<c>Select</c> в анонімний тип та DTO)
    /// і багаторівневого сортування (<c>OrderBy</c>, <c>OrderByDescending</c>, <c>ThenByDescending</c>) у SQLite.
    /// </summary>
    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqlServerDbHelper.RecreateSchema(db, "l6_03");

            Company microsoft = new() { Name = "Microsoft" };
            Company google = new() { Name = "Google" };
            db.Companies.AddRange(microsoft, google);

            db.Users.AddRange(
                new User { Name = "Tom", Age = 36, Company = microsoft },
                new User { Name = "Bob", Age = 39, Company = google },
                new User { Name = "Alice", Age = 28, Company = microsoft },
                new User { Name = "Kate", Age = 28, Company = google }
            );
            db.SaveChanges();
        }

        // 1. Проекція в анонімний тип за допомогою Select
        SqlServerDbHelper.PrintSection("1. Проекція в анонімний тип (Select)");
        using (var db = new AppDbContext())
        {
            // Select вибирає лише потрібні стовпці (зменшує трафік та пам'ять), автоматично генерує JOIN
            // для u.Company!.Name без виклику Include, і НЕ відстежує результат у ChangeTracker
            // (бо повертається анонімний тип, а не сутність моделі).
            var query = db.Users.Select(u => new
            {
                u.Name,
                u.Age,
                Company = u.Company!.Name
            });

            Console.WriteLine("Згенерований SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();

            foreach (var u in query.ToList())
            {
                Console.WriteLine($"{u.Name} ({u.Age}) - {u.Company}");
            }
        }

        // 2. Проекція у власний DTO-клас UserModel
        SqlServerDbHelper.PrintSection("2. Проекція у визначений тип (UserModel)");
        using (var db = new AppDbContext())
        {
            // Проекція в іменований DTO-клас (Data Transfer Object) дає змогу повертати результат
            // із методів сервісів/репозиторіїв (анонімні типи обмежені межами одного методу).
            // Якщо всередині Select явно не включено саму сутність (наприклад, User = u), ChangeTracker її не відстежує.
            var users = db.Users
                .Select(u => new UserModel
                {
                    Name = u.Name,
                    Age = u.Age,
                    Company = u.Company!.Name
                })
                .ToList();

            foreach (UserModel user in users)
            {
                Console.WriteLine($"{user.Name} ({user.Age}) - {user.Company}");
            }
        }

        // 3. Сортування: OrderBy, OrderByDescending, ThenBy, ThenByDescending
        SqlServerDbHelper.PrintSection("3. Сортування (OrderBy, OrderByDescending, ThenBy)");
        using (var db = new AppDbContext())
        {
            // OrderBy транслюється в SQL ORDER BY "u"."Name" ASC.
            // Примітка щодо SQLite: за замовчуванням текстове сортування використовує BINARY collation (чутливе до регістру ASCII),
            // тоді як у MS SQL Server за замовчуванням сортування зазвичай регістронезалежне (CI).
            Console.WriteLine("За зростанням імені (OrderBy):");
            foreach (var u in db.Users.OrderBy(u => u.Name).ToList())
                Console.WriteLine($"  {u.Id}. {u.Name} ({u.Age})");

            Console.WriteLine("\nЗа спаданням імені (OrderByDescending):");
            foreach (var u in db.Users.OrderByDescending(u => u.Name).ToList())
                Console.WriteLine($"  {u.Id}. {u.Name} ({u.Age})");

            // Повторний виклик OrderBy перезаписав би попереднє сортування;
            // для додавання вторинного критерію сортування обов'язково використовуйте ThenBy / ThenByDescending
            // (транслюється в ORDER BY "u"."Age", "c"."Name" DESC).
            Console.WriteLine("\nБагаторівневе сортування: Age (зростання), потім Company.Name (спадання):");
            var sorted = db.Users
                .Include(u => u.Company)
                .OrderBy(u => u.Age)
                .ThenByDescending(u => u.Company!.Name)
                .ToList();

            foreach (var u in sorted)
                Console.WriteLine($"  {u.Name} ({u.Age}) - {u.Company?.Name}");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_10
{
    /// <summary>Компанія (головна сутність).</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Навігаційна колекція користувачів компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Користувач (залежна сутність).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }

        /// <summary>Зовнішній ключ до таблиці Companies.</summary>
        public int CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>DTO-модель (Data Transfer Object) для проекції результатів запиту без відстеження.</summary>
    public class UserModel
    {
        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Назва компанії користувача.</summary>
        public string? Company { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.10 (сортування та проекція).</summary>
    public class AppDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("l6_03");
        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Таблиця компаній.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Налаштовує підключення до локальної БД SQLite для уроку 5.10.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }
    }
}

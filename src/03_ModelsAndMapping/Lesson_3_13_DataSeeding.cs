using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 3.13: Ініціалізація бази даних початковими даними (Data Seeding).
/// Демонструє наповнення таблиць початковими даними через HasData()
/// у методі OnModelCreating, включно зі зв'язаними сутностями (Company -> User).
/// </summary>
public class Lesson_3_13_DataSeeding : ILesson
{
    public string Id => "3.13";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Ініціалізація бази даних початковими даними";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.14.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 13;

    /// <summary>
    /// Запускає демонстрацію початкового заповнення БД (Data Seeding):
    /// перестворює БД (EnsureCreated автоматично вставляє записи з HasData) та виводить
    /// користувачів разом із їхніми компаніями.
    /// </summary>
    public void Run()
    {
        SqlServerDbHelper.PrintSection("3.13. Ініціалізація бази даних початковими даними");

        using var db = new L2_14.ApplicationContext();
        // EnsureCreated() (всередині RecreateDatabase) не лише створює таблиці,
        // а й автоматично виконує INSERT для всіх записів, визначених у HasData().
        // При використанні міграцій (Add-Migration) ці дані потрапляють у метод Up() міграції.
        SqlServerDbHelper.RecreateSchema(db, "l3_13");

        Console.WriteLine("Користувачі, автоматично додані через HasData() при створенні БД:");
        // Include(u => u.Company) завантажує зв'язану компанію одним SQL-запитом (LEFT JOIN),
        // щоб уникнути N+1 запитів або null у навігаційній властивості.
        // AsNoTracking() прибирає накладні витрати Change Tracker, бо дані лише виводяться на екран.
        var users = db.Users
            .Include(u => u.Company)
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToList();

        foreach (var user in users)
        {
            Console.WriteLine($"  [{user.Id}] {user.Name} ({user.Age} р.) — компанія: {user.Company?.Name}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_14
{
    /// <summary>
    /// Сутність користувача, зв'язана з компанією через зовнішній ключ CompanyId.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }

        /// <summary>
        /// Необов'язковий зовнішній ключ до таблиці Companies (nullable int?).
        /// Явна наявність цієї властивості необхідна для зручного задання зв'язків у HasData().
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії-роботодавця.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Сутність компанії (головна сутність у зв'язку 1:N із User).
    /// </summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Колекція співробітників компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>
    /// Контекст бази даних для уроку 2.14, що визначає початкові дані (seed data) у OnModelCreating.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        /// <summary>Набір користувачів (таблиця "Users").</summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>Набір компаній (таблиця "Companies").</summary>
        public DbSet<Company> Companies => Set<Company>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l3_13");
            // Обов'язкова умова HasData(): первинний ключ (Id) має бути вказаний явно!
            // EF Core не генерує ключі під час HasData(), бо ці значення мають бути детермінованими для міграцій.
            var microsoft = new Company { Id = 1, Name = "Microsoft" };
            var google = new Company { Id = 2, Name = "Google" };

            // Для зв'язаних сутностей зв'язок задається виключно через зовнішній ключ (CompanyId),
            // а не через навігаційну властивість Company = microsoft (інакше EF Core кине виняток).
            var tom = new User { Id = 1, Name = "Tom", Age = 23, CompanyId = microsoft.Id };
            var alice = new User { Id = 2, Name = "Alice", Age = 26, CompanyId = microsoft.Id };
            var sam = new User { Id = 3, Name = "Sam", Age = 28, CompanyId = google.Id };

            // Порядок виклику HasData важливий для логіки, хоча EF Core сам впорядковує INSERT за залежностями FK.
            modelBuilder.Entity<Company>().HasData(microsoft, google);
            modelBuilder.Entity<User>().HasData(tom, alice, sam);
        }
    }
}

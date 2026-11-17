using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.14: Ініціалізація бази даних початковими даними (Data Seeding).
/// Демонструє наповнення таблиць початковими даними через HasData()
/// у методі OnModelCreating, включно зі зв'язаними сутностями (Company -> User).
/// </summary>
public class Lesson_2_14_DataSeeding : ILesson
{
    public string Id => "2.14";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Ініціалізація бази даних початковими даними";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.14.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 13;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.14. Ініціалізація бази даних початковими даними");

        using var db = new L2_14.ApplicationContext();
        // EnsureCreated() автоматично виконує вставку даних, визначених у HasData()
        SqliteDbHelper.RecreateDatabase(db);

        Console.WriteLine("Користувачі, автоматично додані через HasData() при створенні БД:");
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
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }

        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Company> Companies => Set<Company>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_14"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Обов'язкова умова HasData(): первинний ключ (Id) має бути вказаний явно!
            var microsoft = new Company { Id = 1, Name = "Microsoft" };
            var google = new Company { Id = 2, Name = "Google" };

            var tom = new User { Id = 1, Name = "Tom", Age = 23, CompanyId = microsoft.Id };
            var alice = new User { Id = 2, Name = "Alice", Age = 26, CompanyId = microsoft.Id };
            var sam = new User { Id = 3, Name = "Sam", Age = 28, CompanyId = google.Id };

            // Для зв'язаних сутностей зв'язок задається через зовнішній ключ (CompanyId),
            // а не через навігаційну властивість Company = microsoft
            modelBuilder.Entity<Company>().HasData(microsoft, google);
            modelBuilder.Entity<User>().HasData(tom, alice, sam);
        }
    }
}

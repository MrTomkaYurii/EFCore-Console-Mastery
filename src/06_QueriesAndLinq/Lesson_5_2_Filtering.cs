using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_2;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.2: Вибірка та фільтрація (Where, EF.Functions.Like, Find).
/// Показує фільтрацію даних на стороні БД, використання шаблонів LIKE та пошук за ключем через Find/FindAsync.
/// </summary>
public class Lesson_5_2_Filtering : ILesson
{
    public string Id => "5.2";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Вибірка та фільтрація (Where, EF.Functions.Like, Find)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.2.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 2;

    public void Run()
    {
        // Початкове наповнення БД
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            Company microsoft = new() { Name = "Microsoft" };
            Company google = new() { Name = "Google" };
            db.Companies.AddRange(microsoft, google);

            db.Users.AddRange(
                new User { Name = "Tom", Age = 36, Company = microsoft },
                new User { Name = "Tomas", Age = 24, Company = google },
                new User { Name = "Bob", Age = 39, Company = google },
                new User { Name = "Alice", Age = 28, Company = microsoft },
                new User { Name = "Kate", Age = 25, Company = google }
            );
            db.SaveChanges();
        }

        // 1. Метод Where та оператор where
        SqliteDbHelper.PrintSection("1. Фільтрація через Where та оператор where");
        using (var db = new AppDbContext())
        {
            // Методи розширення
            var googleUsers = db.Users
                .Where(u => u.Company!.Name == "Google")
                .ToList();

            Console.WriteLine("Користувачі Google (Method Syntax):");
            foreach (var u in googleUsers)
                Console.WriteLine($"  {u.Name} ({u.Age})");

            // Оператори LINQ
            var msUsers = (from u in db.Users
                           where u.Company!.Name == "Microsoft"
                           select u).ToList();

            Console.WriteLine("Користувачі Microsoft (Query Syntax):");
            foreach (var u in msUsers)
                Console.WriteLine($"  {u.Name} ({u.Age})");
        }

        // 2. EF.Functions.Like — пошук за шаблоном SQL LIKE
        SqliteDbHelper.PrintSection("2. Фільтрація через EF.Functions.Like");
        using (var db = new AppDbContext())
        {
            // Пошук усіх користувачів, у імені яких є підрядок "Tom"
            var tomQuery = db.Users.Where(u => EF.Functions.Like(u.Name!, "%Tom%"));
            Console.WriteLine("SQL для Like('%Tom%'):");
            Console.WriteLine(tomQuery.ToQueryString());

            foreach (var u in tomQuery.ToList())
                Console.WriteLine($"  Збіг за ім'ям: {u.Name} ({u.Age})");

            // Пошук за числовим полем через приведення до рядка: вік від 20 до 29 ("2_")
            // Примітка: у SQLite '_' відповідає рівно одному символу, '%' — будь-якій кількості символів
            var twenties = db.Users
                .Where(u => EF.Functions.Like(u.Age.ToString(), "2_"))
                .ToList();

            Console.WriteLine("\nКористувачі віком 20..29 (шаблон '2_'):");
            foreach (var u in twenties)
                Console.WriteLine($"  {u.Name} ({u.Age})");
        }

        // 3. Пошук одного об'єкта за первинним ключем: Find / FindAsync
        SqliteDbHelper.PrintSection("3. Пошук за первинним ключем через Find та FindAsync");
        using (var db = new AppDbContext())
        {
            // Find спочатку перевіряє локальний кеш контексту (ChangeTracker), а потім робить запит до БД
            User? user3 = db.Users.Find(3);
            if (user3 is not null)
                Console.WriteLine($"Find(3): {user3.Name} ({user3.Age})");

            // Повторний виклик Find(3) поверне об'єкт із кешу без SQL-запиту до БД
            User? cachedUser3 = db.Users.Find(3);
            Console.WriteLine($"Повторний Find(3) — той самий екземпляр у пам'яті: {ReferenceEquals(user3, cachedUser3)}");

            // Асинхронний варіант (у консольному синхронному уроці викликаємо .AsTask().GetAwaiter().GetResult())
            User? user1 = db.Users.FindAsync(1).AsTask().GetAwaiter().GetResult();
            if (user1 is not null)
                Console.WriteLine($"FindAsync(1): {user1.Name} ({user1.Age})");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_2
{
    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
    }

    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_2"));
        }
    }
}

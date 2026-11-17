using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_7;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.7: Відстеження об'єктів та AsNoTracking.
/// Демонструє різницю між відстежуваними та невідстежуваними запитами,
/// а також глобальне вимкнення відстеження через ChangeTracker.QueryTrackingBehavior.
/// </summary>
public class Lesson_5_7_TrackingAndAsNoTracking : ILesson
{
    public string Id => "5.7";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Відстеження об'єктів та AsNoTracking";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.7.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 7;

    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            db.Users.AddRange(
                new User { Name = "Tom", Age = 36 },
                new User { Name = "Bob", Age = 39 },
                new User { Name = "Alice", Age = 28 },
                new User { Name = "Kate", Age = 25 }
            );
            db.SaveChanges();
        }

        // 1. Стандартний відстежуваний запит (Tracking)
        SqliteDbHelper.PrintSection("1. Відстежуваний запит (за замовчуванням)");
        using (var db = new AppDbContext())
        {
            var user = db.Users.FirstOrDefault();
            if (user is not null)
            {
                user.Age = 18;
                Console.WriteLine($"Відстежується сутностей у ChangeTracker: {db.ChangeTracker.Entries().Count()}");
                db.SaveChanges(); // Зміна зберігається в БД
            }

            foreach (var u in db.Users.ToList())
                Console.WriteLine($"  {u.Name} ({u.Age})");
        }

        // 2. Невідстежуваний запит із AsNoTracking()
        SqliteDbHelper.PrintSection("2. Невідстежуваний запит через AsNoTracking()");
        using (var db = new AppDbContext())
        {
            var user = db.Users.AsNoTracking().FirstOrDefault();
            if (user is not null)
            {
                user.Age = 99; // Зміна лише в пам'яті — ChangeTracker про неї не знає!
                Console.WriteLine($"Відстежується сутностей у ChangeTracker: {db.ChangeTracker.Entries().Count()}");
                int affected = db.SaveChanges();
                Console.WriteLine($"SaveChanges() оновив рядків: {affected}");
            }

            foreach (var u in db.Users.AsNoTracking().ToList())
                Console.WriteLine($"  {u.Name} ({u.Age}) — у БД залишилось 18");
        }

        // 3. Вимкнення відстеження для всього контексту через ChangeTracker.QueryTrackingBehavior
        SqliteDbHelper.PrintSection("3. Глобальне налаштування ChangeTracker.QueryTrackingBehavior = NoTracking");
        using (var db = new AppDbContext())
        {
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var user = db.Users.FirstOrDefault();
            if (user is not null)
            {
                user.Age = 77;
                int affected = db.SaveChanges();
                Console.WriteLine($"SaveChanges() після глобального NoTracking оновив рядків: {affected}");
            }

            // Якщо все ж потрібно відстежити конкретний запит, можна викликати .AsTracking()
            var trackedUser = db.Users.AsTracking().FirstOrDefault();
            Console.WriteLine($"Після AsTracking() у ChangeTracker записів: {db.ChangeTracker.Entries().Count()} (перший: {trackedUser?.Name})");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_7
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_7"));
        }
    }
}

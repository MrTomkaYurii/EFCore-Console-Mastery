using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_7;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 6.7: Відстеження об'єктів та AsNoTracking.
/// Демонструє різницю між відстежуваними та невідстежуваними запитами,
/// а також глобальне вимкнення відстеження через ChangeTracker.QueryTrackingBehavior.
/// </summary>
public class Lesson_6_07_TrackingAndAsNoTracking : ILesson
{
    public string Id => "6.7";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Відстеження об'єктів та AsNoTracking";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.7.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 7;

    /// <summary>
    /// Запускає демонстрацію режимів відстеження змін (Tracking vs AsNoTracking),
    /// глобального налаштування <see cref="ChangeTracker.QueryTrackingBehavior"/> та локального перевизначення через <c>AsTracking()</c>.
    /// </summary>
    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqlServerDbHelper.RecreateSchema(db, "l6_07");

            db.Users.AddRange(
                new User { Name = "Tom", Age = 36 },
                new User { Name = "Bob", Age = 39 },
                new User { Name = "Alice", Age = 28 },
                new User { Name = "Kate", Age = 25 }
            );
            db.SaveChanges();
        }

        // 1. Стандартний відстежуваний запит (Tracking)
        SqlServerDbHelper.PrintSection("1. Відстежуваний запит (за замовчуванням)");
        using (var db = new AppDbContext())
        {
            // За замовчуванням EF Core зберігає знімок (snapshot) початкових значень кожної завантаженої сутності у ChangeTracker.
            // Коли викликається SaveChanges(), EF Core порівнює поточні властивості зі знімком і генерує UPDATE лише для змінених колонок.
            var user = db.Users.FirstOrDefault();
            if (user is not null)
            {
                user.Age = 18;
                Console.WriteLine($"Відстежується сутностей у ChangeTracker: {db.ChangeTracker.Entries().Count()}");
                db.SaveChanges(); // Генерує UPDATE "Users" SET "Age" = @p0 WHERE "Id" = @p1
            }

            foreach (var u in db.Users.ToList())
                Console.WriteLine($"  {u.Name} ({u.Age})");
        }

        // 2. Невідстежуваний запит із AsNoTracking()
        SqlServerDbHelper.PrintSection("2. Невідстежуваний запит через AsNoTracking()");
        using (var db = new AppDbContext())
        {
            // AsNoTracking() ідеально підходить для сценаріїв «тільки читання» (read-only: звіти, API GET-запити),
            // оскільки не створює snapshot у ChangeTracker — це зменшує споживання пам'яті та прискорює матеріалізацію.
            var user = db.Users.AsNoTracking().FirstOrDefault();
            if (user is not null)
            {
                user.Age = 99; // Зміна лише в пам'яті — ChangeTracker про неї не знає!
                Console.WriteLine($"Відстежується сутностей у ChangeTracker: {db.ChangeTracker.Entries().Count()}");
                int affected = db.SaveChanges(); // Поверне 0, жодного SQL UPDATE не буде відправлено до SQLite
                Console.WriteLine($"SaveChanges() оновив рядків: {affected}");
            }

            foreach (var u in db.Users.AsNoTracking().ToList())
                Console.WriteLine($"  {u.Name} ({u.Age}) — у БД залишилось 18");
        }

        // 3. Вимкнення відстеження для всього контексту через ChangeTracker.QueryTrackingBehavior
        SqlServerDbHelper.PrintSection("3. Глобальне налаштування ChangeTracker.QueryTrackingBehavior = NoTracking");
        using (var db = new AppDbContext())
        {
            // Корисно для read-only сервісів або CQRS Query-обробників;
            // альтернативно це можна задати в OnConfiguring через optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var user = db.Users.FirstOrDefault();
            if (user is not null)
            {
                user.Age = 77;
                int affected = db.SaveChanges();
                Console.WriteLine($"SaveChanges() після глобального NoTracking оновив рядків: {affected}");
            }

            // Якщо все ж потрібно відстежити конкретний запит (наприклад, для подальшого оновлення),
            // можна локально увімкнути відстеження через .AsTracking()
            var trackedUser = db.Users.AsTracking().FirstOrDefault();
            Console.WriteLine($"Після AsTracking() у ChangeTracker записів: {db.ChangeTracker.Entries().Count()} (перший: {trackedUser?.Name})");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_7
{
    /// <summary>Користувач (сутність для демонстрації відстеження змін у ChangeTracker).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.7 (Tracking та AsNoTracking).</summary>
    public class AppDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("l6_07");
        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Налаштовує підключення до локальної БД SQLite для уроку 5.7.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }
    }
}

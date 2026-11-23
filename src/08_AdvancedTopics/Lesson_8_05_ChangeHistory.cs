using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch08;

/// <summary>
/// Урок 8.5: Зберігання історії змін — нативні Temporal Tables у MS SQL Server (IsTemporal, TemporalAll, TemporalAsOf).
/// </summary>
public class Lesson_8_05_ChangeHistory : ILesson
{
    public string Id => "8.5";
    public string Chapter => "08. Додаткові теми";
    public string Title => "Зберігання історії змін (MS SQL Server Temporal Tables)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/8.3.php";
    public int ChapterOrder => 8;
    public int LessonOrder => 5;

    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class TemporalContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l8_05");
            // Вмикаємо системно-версіоновану таблицю MS SQL Server (SYSTEM_VERSIONING = ON)
            modelBuilder.Entity<User>().ToTable("Users", "l8_05", t => t.IsTemporal());
        }
    }

    public void Run()
    {
        using (var setupDb = new TemporalContext())
        {
            SqlServerDbHelper.RecreateSchema(setupDb, "l8_05");
        }

        SqlServerDbHelper.PrintSection("1. Внесення змін у Temporal Table (INSERT -> UPDATE -> UPDATE -> DELETE)");
        int userId;
        DateTime afterFirstInsertUtc;

        using (var db = new TemporalContext())
        {
            var tom = new User { Name = "Tom" };
            db.Users.Add(tom);
            db.SaveChanges();
            userId = tom.Id;
            Console.WriteLine($"  Додано: Id={userId}, Name='Tom'");
            Thread.Sleep(50);
            afterFirstInsertUtc = DateTime.UtcNow;
            Thread.Sleep(50);

            tom.Name = "Bob";
            db.SaveChanges();
            Console.WriteLine($"  Оновлено: Id={userId}, Name='Bob'");
            Thread.Sleep(50);

            tom.Name = "Sam";
            db.SaveChanges();
            Console.WriteLine($"  Оновлено: Id={userId}, Name='Sam'");
            Thread.Sleep(50);

            db.Users.Remove(tom);
            db.SaveChanges();
            Console.WriteLine($"  Видалено: Id={userId}");
        }

        SqlServerDbHelper.PrintSection("2. Повна історія змін через TemporalAll()");
        using (var db = new TemporalContext())
        {
            var history = db.Users
                .TemporalAll()
                .Where(u => u.Id == userId)
                .OrderBy(u => EF.Property<DateTime>(u, "PeriodStart"))
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    PeriodStart = EF.Property<DateTime>(u, "PeriodStart"),
                    PeriodEnd = EF.Property<DateTime>(u, "PeriodEnd")
                })
                .ToList();

            foreach (var h in history)
                Console.WriteLine($"  Id={h.Id}, Name='{h.Name}' | {h.PeriodStart:HH:mm:ss.fff} .. {h.PeriodEnd:HH:mm:ss.fff}");

            SqlServerDbHelper.PrintSection("3. Зріз стану на момент часу через TemporalAsOf(...)");
            var snapshot = db.Users
                .TemporalAsOf(afterFirstInsertUtc)
                .FirstOrDefault(u => u.Id == userId);
            Console.WriteLine($"  Стан на {afterFirstInsertUtc:HH:mm:ss.fff} UTC: Name='{snapshot?.Name}'");
        }
    }
}

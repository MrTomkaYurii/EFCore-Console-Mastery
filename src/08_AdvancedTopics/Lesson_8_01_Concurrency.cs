using System.ComponentModel.DataAnnotations;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch08;

/// <summary>
/// Урок 8.1: Паралелізм та оптимістичне блокування в MS SQL Server ([ConcurrencyCheck] та [Timestamp] / rowversion).
/// </summary>
public class Lesson_8_01_Concurrency : ILesson
{
    public string Id => "8.1";
    public string Chapter => "08. Додаткові теми";
    public string Title => "Паралелізм та оптимістичне блокування (Concurrency)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.11.php";
    public int ChapterOrder => 8;
    public int LessonOrder => 1;

    private sealed class UserWithCheck
    {
        public int Id { get; set; }

        [ConcurrencyCheck]
        public string? Name { get; set; }
    }

    private sealed class UserWithRowVersion
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        // MS SQL Server автоматично оновлює стовпець типу rowversion (8 байт) при кожному UPDATE!
        [Timestamp]
        public byte[] RowVersion { get; set; } = [];
    }

    private sealed class ConcurrencyContext : DbContext
    {
        public DbSet<UserWithCheck> UsersWithCheck => Set<UserWithCheck>();
        public DbSet<UserWithRowVersion> UsersWithRowVersion => Set<UserWithRowVersion>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.HasDefaultSchema("l8_01");
    }

    public void Run()
    {
        using (var setupDb = new ConcurrencyContext())
        {
            SqlServerDbHelper.RecreateSchema(setupDb, "l8_01");
            setupDb.UsersWithCheck.Add(new UserWithCheck { Name = "Tom" });
            setupDb.UsersWithRowVersion.Add(new UserWithRowVersion { Name = "Alice" });
            setupDb.SaveChanges();
        }

        SqlServerDbHelper.PrintSection("1. Конфлікт паралелізму з [ConcurrencyCheck]");
        using (var db1 = new ConcurrencyContext())
        using (var db2 = new ConcurrencyContext())
        {
            var u1 = db1.UsersWithCheck.First();
            var u2 = db2.UsersWithCheck.First();

            u1.Name = "Bob";
            db1.SaveChanges();
            Console.WriteLine("  db1 успішно оновив Name -> Bob");

            u2.Name = "Sam";
            try
            {
                db2.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                Console.WriteLine("  [DbUpdateConcurrencyException] db2 не зміг перезаписати змінений рядок!");
            }
        }

        SqlServerDbHelper.PrintSection("2. Нативний [Timestamp] (rowversion) у MS SQL Server");
        using (var db1 = new ConcurrencyContext())
        using (var db2 = new ConcurrencyContext())
        {
            var u1 = db1.UsersWithRowVersion.First();
            var u2 = db2.UsersWithRowVersion.First();
            Console.WriteLine($"  Початковий RowVersion: 0x{Convert.ToHexString(u1.RowVersion)}");

            u1.Name = "Alice Updated";
            db1.SaveChanges();
            Console.WriteLine($"  Після db1.SaveChanges(): 0x{Convert.ToHexString(u1.RowVersion)} (оновлено сервером SQL Server)");

            u2.Name = "Alice Stale";
            try
            {
                db2.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                Console.WriteLine("  [DbUpdateConcurrencyException] MS SQL Server заблокував застарілий RowVersion!");
            }
        }
    }
}

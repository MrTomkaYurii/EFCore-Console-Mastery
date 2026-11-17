using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 2.16. Управління базою даних (DatabaseFacade).
/// Демонстрація EnsureDeleted, EnsureCreated, CanConnect та їхніх асинхронних версій.
/// </summary>
public class Lesson_2_16_DatabaseManagement : ILesson
{
    public string Id => "2.16";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Управління базою даних";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.16.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 4;

    public void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        var connectionString = SqliteDbHelper.GetConnectionString("lesson_2_16");

        using var db = new L2_16.ManagementContext(connectionString);

        // 1. Синхронні методи EnsureDeleted та EnsureCreated
        SqliteDbHelper.PrintSection("1. Синхронне видалення та створення БД");

        bool deletedBefore = db.Database.EnsureDeleted();
        Console.WriteLine($"EnsureDeleted() (очищення перед стартом): {deletedBefore}");

        // У SQLite CanConnect() поверне true лише якщо файл фізично існує (або буде створений)
        // Важливо: для SQLite провайдер може перевіряти можливість відкрити файл, тому перевіримо File.Exists + CanConnect
        bool createdFirst = db.Database.EnsureCreated();
        Console.WriteLine($"EnsureCreated() (перший виклик): {createdFirst} -> БД щойно створена");

        bool createdSecond = db.Database.EnsureCreated();
        Console.WriteLine($"EnsureCreated() (повторний виклик): {createdSecond} -> БД уже існує");

        // 2. Перевірка доступності БД
        SqliteDbHelper.PrintSection("2. Перевірка доступності (CanConnect)");
        bool isAvailable = db.Database.CanConnect();
        Console.WriteLine(isAvailable
            ? "CanConnect(): База даних доступна"
            : "CanConnect(): База даних недоступна");

        // 3. Асинхронні варіанти методів DatabaseFacade
        SqliteDbHelper.PrintSection("3. Асинхронні методи (EnsureDeletedAsync / EnsureCreatedAsync / CanConnectAsync)");

        bool deletedAsync = await db.Database.EnsureDeletedAsync();
        Console.WriteLine($"EnsureDeletedAsync(): {deletedAsync}");

        bool createdAsync = await db.Database.EnsureCreatedAsync();
        Console.WriteLine($"EnsureCreatedAsync(): {createdAsync}");

        bool canConnectAsync = await db.Database.CanConnectAsync();
        Console.WriteLine($"CanConnectAsync(): {canConnectAsync}");
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch01.L2_16
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class ManagementContext(string connectionString) : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}

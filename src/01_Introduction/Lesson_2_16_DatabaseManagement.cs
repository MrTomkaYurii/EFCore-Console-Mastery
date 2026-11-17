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

    /// <summary>
    /// Запускає демонстрацію методів керування життєвим циклом бази даних через <c>db.Database</c> (DatabaseFacade):
    /// синхронних та асинхронних версій EnsureDeleted, EnsureCreated і CanConnect.
    /// </summary>
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

        // EnsureDeleted() у SQLite фізично видаляє файл .db (якщо він існує) і повертає true;
        // якщо файлу не було — повертає false. (У MS SQL Server виконується DROP DATABASE).
        bool deletedBefore = db.Database.EnsureDeleted();
        Console.WriteLine($"EnsureDeleted() (очищення перед стартом): {deletedBefore}");

        // EnsureCreated() перевіряє, чи існує БД і чи є в ній таблиці:
        // - Якщо БД немає — створює файл SQLite та всі таблиці згідно з поточною моделлю (без міграцій!) і повертає true.
        // - Важливе обмеження: EnsureCreated() НЕ сумісний із міграціями (не створює таблицю __EFMigrationsHistory)
        //   і не оновлює схему, якщо БД вже існує з іншою структурою.
        bool createdFirst = db.Database.EnsureCreated();
        Console.WriteLine($"EnsureCreated() (перший виклик): {createdFirst} -> БД щойно створена");

        // Повторний виклик бачить, що таблиці вже існують, нічого не змінює та повертає false
        bool createdSecond = db.Database.EnsureCreated();
        Console.WriteLine($"EnsureCreated() (повторний виклик): {createdSecond} -> БД уже існує");

        // 2. Перевірка доступності БД
        SqliteDbHelper.PrintSection("2. Перевірка доступності (CanConnect)");
        // CanConnect() намагається відкрити з'єднання без викидання винятку (зручно для Health Checks).
        // Особливість SQLite: провайдер перевіряє існування файлу БД (Mode=ReadWriteCreate за замовчуванням може
        // поводитися інакше при ручному відкритті, але CanConnect у EF Core SQLite повертає false, якщо файлу немає).
        bool isAvailable = db.Database.CanConnect();
        Console.WriteLine(isAvailable
            ? "CanConnect(): База даних доступна"
            : "CanConnect(): База даних недоступна");

        // 3. Асинхронні варіанти методів DatabaseFacade
        // Корисні для веб-застосунків та асинхронної ініціалізації без блокування потоку
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
    /// <summary>
    /// Мінімальна модель користувача для демонстрації створення та видалення таблиці через DatabaseFacade.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Первинний ключ таблиці Users (INTEGER PRIMARY KEY AUTOINCREMENT у SQLite).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача (TEXT NULL у SQLite).
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Контекст даних для демонстрації методів <see cref="DbContext.Database"/>.
    /// </summary>
    public class ManagementContext(string connectionString) : DbContext
    {
        /// <summary>
        /// Набір користувачів, що відповідає таблиці "Users".
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Налаштовує провайдер SQLite для роботи з файлом БД.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}

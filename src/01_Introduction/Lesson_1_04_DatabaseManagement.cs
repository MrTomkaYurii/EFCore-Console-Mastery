using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01;

/// <summary>
/// Урок 1.4. Управління базою даних (DatabaseFacade: CanConnect, EnsureCreated, EnsureDeleted).
/// </summary>
public class Lesson_1_04_DatabaseManagement : ILesson
{
    public string Id => "1.4";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Управління базою даних";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.16.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 4;

    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class TempDbContext : DbContext
    {
        private readonly string _connStr;
        public DbSet<User> Users => Set<User>();

        public TempDbContext(string connStr) => _connStr = connStr;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlServer(_connStr);
    }

    public void Run() => RunAsync().GetAwaiter().GetResult();

    private static async Task RunAsync()
    {
        // Для безпечної демонстрації EnsureDeleted / EnsureCreated використовуємо окрему тимчасову БД
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(SqlServerDbHelper.ConnectionString)
        {
            InitialCatalog = "EFCoreMastery_TempDemo"
        };
        var tempConnStr = builder.ConnectionString;

        SqlServerDbHelper.PrintSection("1. Синхронне видалення та створення БД (EnsureDeleted / EnsureCreated)");
        using (var db = new TempDbContext(tempConnStr))
        {
            bool deleted = db.Database.EnsureDeleted();
            bool createdFirst = db.Database.EnsureCreated();
            bool createdSecond = db.Database.EnsureCreated();

            Console.WriteLine($"EnsureDeleted():        {deleted}");
            Console.WriteLine($"EnsureCreated() (1-й):  {createdFirst}");
            Console.WriteLine($"EnsureCreated() (2-й):  {createdSecond} (БД вже існує)");
        }

        SqlServerDbHelper.PrintSection("2. Перевірка доступності (CanConnect)");
        using (var db = new TempDbContext(tempConnStr))
        {
            Console.WriteLine($"CanConnect():           {db.Database.CanConnect()}");
        }

        SqlServerDbHelper.PrintSection("3. Асинхронні методи (EnsureDeletedAsync / EnsureCreatedAsync / CanConnectAsync)");
        await using (var db = new TempDbContext(tempConnStr))
        {
            bool deletedAsync = await db.Database.EnsureDeletedAsync();
            bool createdAsync = await db.Database.EnsureCreatedAsync();
            bool canConnectAsync = await db.Database.CanConnectAsync();

            Console.WriteLine($"EnsureDeletedAsync():   {deletedAsync}");
            Console.WriteLine($"EnsureCreatedAsync():   {createdAsync}");
            Console.WriteLine($"CanConnectAsync():      {canConnectAsync}");

            // Прибираємо тимчасову БД після демонстрації
            await db.Database.EnsureDeletedAsync();
        }
    }
}

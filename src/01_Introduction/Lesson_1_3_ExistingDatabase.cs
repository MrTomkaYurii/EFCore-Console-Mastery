using EFCoreConsoleMastery.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 1.3. Підключення до існуючої бази даних (Database First).
/// Спочатку створюємо та наповнюємо SQLite БД напряму через SQL (без EF),
/// а потім підключаємо до неї новий DbContext БЕЗ виклику EnsureCreated().
/// </summary>
public class Lesson_1_3_ExistingDatabase : ILesson
{
    public string Id => "1.3";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Підключення до існуючої бази даних";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.3.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 3;

    public void Run()
    {
        const string dbName = "lesson_1_3_existing";
        var dbPath = SqliteDbHelper.GetDbPath(dbName);
        var connectionString = SqliteDbHelper.GetConnectionString(dbName);

        // Видаляємо попередній файл, щоб імітувати "існуючу зовнішню БД" з нуля
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }

        // Крок 1: Створюємо "існуючу" базу даних засобами чистого ADO.NET (Microsoft.Data.Sqlite)
        SqliteDbHelper.PrintSection("1. Створення існуючої БД через сирий SQL (без EF Core)");
        using (var rawConnection = new SqliteConnection(connectionString))
        {
            rawConnection.Open();

            using var createCmd = rawConnection.CreateCommand();
            createCmd.CommandText = """
                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NULL,
                    Age INTEGER NOT NULL
                );
                INSERT INTO Users (Name, Age) VALUES ('Tom', 33);
                INSERT INTO Users (Name, Age) VALUES ('Alice', 26);
                """;
            createCmd.ExecuteNonQuery();
            Console.WriteLine($"Створено таблицю Users та додано 2 рядки у файл: {Path.GetFileName(dbPath)}");
        }

        // Крок 2: Підключаємося до вже існуючої БД через DbContext БЕЗ EnsureCreated()
        SqliteDbHelper.PrintSection("2. Підключення EF Core DbContext до існуючої БД");
        using (var db = new L1_3.ExistingDbContext(connectionString))
        {
            // Зверніть увагу: EnsureCreated() тут НЕ викликається!
            var existingUsers = db.Users.ToList();
            Console.WriteLine("Прочитано існуючі записи через EF Core:");
            foreach (var u in existingUsers)
            {
                Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");
            }

            // Можемо одразу працювати з таблицею — додамо ще один запис
            db.Users.Add(new L1_3.User { Name = "Bob", Age = 29 });
            db.SaveChanges();

            Console.WriteLine("\nПісля додавання нового користувача через EF Core:");
            foreach (var u in db.Users.ToList())
            {
                Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch01.L1_3
{
    /// <summary>
    /// Клас моделі, що відповідає структурі таблиці Users у вже існуючій БД.
    /// У реальному проєкті такий клас генерується командою Scaffold-DbContext / dotnet ef dbcontext scaffold.
    /// Зверніть увагу: у SQLite INTEGER зазвичай відображається скафолдером як long.
    /// </summary>
    public partial class User
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public long Age { get; set; }
    }

    /// <summary>
    /// Контекст для існуючої БД. Конструктор НЕ викликає Database.EnsureCreated().
    /// </summary>
    public partial class ExistingDbContext : DbContext
    {
        private readonly string _connectionString;

        public virtual DbSet<User> Users { get; set; } = null!;

        public ExistingDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

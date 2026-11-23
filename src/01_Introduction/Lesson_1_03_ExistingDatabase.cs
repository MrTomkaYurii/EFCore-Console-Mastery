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
public class Lesson_1_03_ExistingDatabase : ILesson
{
    public string Id => "1.3";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Підключення до існуючої бази даних";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.3.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 3;

    /// <summary>
    /// Запускає демонстрацію підходу Database First:
    /// 1) Створює файл SQLite та таблицю Users через "сирий" ADO.NET (без EF Core);
    /// 2) Підключає до вже існуючої БД контекст EF Core без виклику EnsureCreated(), читає та додає дані.
    /// </summary>
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
            // У SQLite Open() автоматично створює фізичний файл БД, якщо його ще не існує
            // (на відміну від MS SQL Server, де БД має бути попередньо створена на сервері)
            rawConnection.Open();

            using var createCmd = rawConnection.CreateCommand();
            // Використовуємо синтаксис SQLite: INTEGER PRIMARY KEY AUTOINCREMENT (у SQL Server це INT IDENTITY(1,1))
            // та TEXT NULL (у SQL Server це NVARCHAR(MAX) NULL).
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
            // Якби схема була іншою або таблиці вже містили б дані, EnsureCreated() просто нічого не зробив би,
            // але у підході Database First ми покладаємося на те, що таблиця вже існує у БД.
            var existingUsers = db.Users.ToList();
            Console.WriteLine("Прочитано існуючі записи через EF Core:");
            foreach (var u in existingUsers)
            {
                Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");
            }

            // Можемо одразу працювати з таблицею — додамо ще один запис через EF Core.
            // EF Core згенерує INSERT INTO "Users" ... і SQLite автоматично призначить Id = 3 (через AUTOINCREMENT).
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
    /// У реальному проєкті такий клас генерується командою Scaffold-DbContext / dotnet ef dbcontext scaffold
    /// із ключем partial, щоб модель можна було розширювати в окремому файлі без втрати коду при повторному скафолдингу.
    /// Зверніть увагу: у SQLite будь-який INTEGER є 64-бітним, тому скафолдер EF Core відображає його як long (Int64),
    /// тоді як у MS SQL Server INT відображається як int (Int32), а BIGINT — як long.
    /// </summary>
    public partial class User
    {
        /// <summary>
        /// Первинний ключ таблиці Users (INTEGER у SQLite -> long у C# після scaffold).
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Ім'я користувача (TEXT NULL у SQLite -> string? у C#).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Вік користувача (INTEGER NOT NULL у SQLite -> long у C# після scaffold).
        /// </summary>
        public long Age { get; set; }
    }

    /// <summary>
    /// Контекст для існуючої БД (імітує код, згенерований за допомогою Scaffold-DbContext).
    /// Конструктор НЕ викликає Database.EnsureCreated(), оскільки схема вже створена в БД.
    /// </summary>
    public partial class ExistingDbContext : DbContext
    {
        private readonly string _connectionString;

        /// <summary>
        /// Набір сутностей для таблиці Users.
        /// Оголошено як virtual (стандарт скафолдингу EF Core) для можливості мокування в unit-тестах.
        /// </summary>
        public virtual DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Ініціалізує контекст із рядком підключення до існуючої БД.
        /// </summary>
        public ExistingDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Налаштовує провайдер SQLite для підключення до існуючої бази даних.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }

        /// <summary>
        /// Використовується для конфігурації моделі через Fluent API (при скафолдингу сюди генеруються точні імена таблиць, обмеження та індекси).
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Виклик partial-методу дозволяє додавати власну Fluent API конфігурацію в іншому файлі
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

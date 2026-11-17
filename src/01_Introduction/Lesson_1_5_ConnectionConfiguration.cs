using System.Text;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 1.5. Конфігурація підключення.
/// Демонстрація способів налаштування DbContext:
/// 1) Через OnConfiguring(DbContextOptionsBuilder) з передачею рядка підключення ззовні;
/// 2) Через конструктор DbContext(DbContextOptions&lt;TContext&gt;) та DbContextOptionsBuilder;
/// 3) Читання конфігурації через ConfigurationBuilder з JSON (in-memory JSON stream).
/// </summary>
public class Lesson_1_5_ConnectionConfiguration : ILesson
{
    public string Id => "1.5";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Конфігурація підключення";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.5.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 6;

    /// <summary>
    /// Запускає демонстрацію трьох способів налаштування підключення до БД у EF Core:
    /// 1) через метод OnConfiguring;
    /// 2) через передачу DbContextOptions&lt;TContext&gt; у конструктор (патерн для DI);
    /// 3) через зчитування рядка підключення з JSON-конфігурації за допомогою ConfigurationBuilder.
    /// </summary>
    public void Run()
    {
        // Спосіб 1: Конфігурація через OnConfiguring з передачею рядка підключення в конструктор.
        // Зручно для простих консольних програм, але жорстко прив'язує контекст до виклику UseSqlite().
        SqliteDbHelper.PrintSection("1. Конфігурація через OnConfiguring");
        var connStr1 = SqliteDbHelper.GetConnectionString("lesson_1_5_onconfiguring");
        using (var db1 = new L1_5.OnConfiguringContext(connStr1))
        {
            SqliteDbHelper.RecreateDatabase(db1);
            db1.Users.Add(new L1_5.User { Name = "Tom", Age = 33 });
            db1.SaveChanges();

            // Count() транслюється у SELECT COUNT(*) FROM "Users" — агрегація виконується на боці SQLite
            Console.WriteLine($"Прочитано з БД #1: {db1.Users.Count()} запис(ів)");
        }

        // Спосіб 2: Передача DbContextOptions<T> у конструктор базового класу.
        // Це стандартний підхід для ASP.NET Core та DI-контейнера (AddDbContext<T>), оскільки дозволяє
        // підміняти провайдер ззовні (наприклад, SQLite у тестах і SQL Server/PostgreSQL у продакшені).
        SqliteDbHelper.PrintSection("2. Конфігурація через DbContextOptions<T>");
        var connStr2 = SqliteDbHelper.GetConnectionString("lesson_1_5_options");
        var optionsBuilder = new DbContextOptionsBuilder<L1_5.OptionsContext>();
        var options = optionsBuilder.UseSqlite(connStr2).Options;

        using (var db2 = new L1_5.OptionsContext(options))
        {
            SqliteDbHelper.RecreateDatabase(db2);
            db2.Users.Add(new L1_5.User { Name = "Alice", Age = 26 });
            db2.SaveChanges();

            foreach (var u in db2.Users.ToList())
            {
                Console.WriteLine($"  БД #2 -> {u.Id}. {u.Name} - {u.Age}");
            }
        }

        // Спосіб 3: Завантаження рядка підключення з JSON-конфігурації (IConfiguration).
        // У реальних застосунках рядки підключення не хардкодять у C#-коді, а виносять у appsettings.json,
        // змінні середовища або Secret Manager.
        SqliteDbHelper.PrintSection("3. ConfigurationBuilder + JSON-конфігурація");
        var connStr3 = SqliteDbHelper.GetConnectionString("lesson_1_5_json");

        // Імітуємо файл appsettings.json у пам'яті, щоб не засмічувати робочий каталог.
        // Екрануємо зворотні слеші (\) у шляху Windows, щоб JSON залишався валідним.
        var escapedConnStr = connStr3.Replace("\\", "\\\\");
        var jsonConfig = $$"""
            {
              "ConnectionStrings": {
                "DefaultConnection": "{{escapedConnStr}}"
              }
            }
            """;

        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonConfig));
        IConfiguration config = new ConfigurationBuilder()
            // AddJsonStream зчитує JSON-конфігурацію з потоку (аналог AddJsonFile("appsettings.json"))
            .AddJsonStream(jsonStream)
            .Build();

        // GetConnectionString("DefaultConnection") — скорочення для config.GetSection("ConnectionStrings")["DefaultConnection"]
        var resolvedConnStr = config.GetConnectionString("DefaultConnection");
        Console.WriteLine($"Отриманий рядок підключення з конфігурації: {resolvedConnStr}");

        var jsonOptions = new DbContextOptionsBuilder<L1_5.OptionsContext>()
            .UseSqlite(resolvedConnStr)
            .Options;

        using (var db3 = new L1_5.OptionsContext(jsonOptions))
        {
            SqliteDbHelper.RecreateDatabase(db3);
            db3.Users.Add(new L1_5.User { Name = "Bob", Age = 40 });
            db3.SaveChanges();

            foreach (var u in db3.Users.ToList())
            {
                Console.WriteLine($"  БД #3 -> {u.Id}. {u.Name} - {u.Age}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch01.L1_5
{
    /// <summary>
    /// Сутність користувача, спільна для всіх трьох способів конфігурації.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Первинний ключ (INTEGER PRIMARY KEY AUTOINCREMENT у SQLite).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача (TEXT NULL у SQLite).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Вік користувача (INTEGER NOT NULL у SQLite).
        /// </summary>
        public int Age { get; set; }
    }

    /// <summary>
    /// Варіант 1: Налаштування провайдера всередині OnConfiguring.
    /// Якщо OnConfiguring викликається без перевірки !optionsBuilder.IsConfigured,
    /// контекст завжди використовуватиме задану тут конфігурацію.
    /// </summary>
    public class OnConfiguringContext(string connectionString) : DbContext
    {
        /// <summary>
        /// Таблиця користувачів у БД.
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Налаштовує провайдер SQLite безпосередньо в класі контексту.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString);
        }
    }

    /// <summary>
    /// Варіант 2: Передача готових DbContextOptions ззовні (стандарт для ASP.NET Core та DI).
    /// Такий контекст не залежить від конкретного провайдера на рівні свого коду.
    /// </summary>
    public class OptionsContext(DbContextOptions<OptionsContext> options) : DbContext(options)
    {
        /// <summary>
        /// Таблиця користувачів у БД.
        /// </summary>
        public DbSet<User> Users => Set<User>();
    }
}

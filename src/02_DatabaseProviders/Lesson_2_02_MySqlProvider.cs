using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch02;

/// <summary>
/// Урок 2.2: Провайдер MySQL (та адаптація під SQLite).
/// Оригінал: https://metanit.com/sharp/efcore/7.2.php
/// </summary>
public class Lesson_2_02_MySqlProvider : ILesson
{
    public string Id => "2.2";
    public string Chapter => "02. Провайдери баз даних";
    public string Title => "Провайдер MySQL (та адаптація під SQLite)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/7.2.php";
    public int ChapterOrder => 2;
    public int LessonOrder => 2;

    // Ізольована модель User для цього уроку
    /// <summary>
    /// Модель користувача, яка відображається на таблицю Users у БД.
    /// </summary>
    private sealed class User
    {
        /// <summary>
        /// Первинний ключ сутності. За конвенцією EF Core перетворюється на автоінкрементний стовпець
        /// (AUTO_INCREMENT у MySQL, INTEGER PRIMARY KEY AUTOINCREMENT у SQLite, IDENTITY у MS SQL Server).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача (nullable). У MySQL за замовчуванням відображається в LONGTEXT NULL,
        /// якщо не обмежити довжину через [MaxLength] або HasMaxLength(), а в SQLite — у TEXT NULL.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Вік користувача (значущий тип int, тому в БД створюється стовпець NOT NULL).
        /// </summary>
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст даних, налаштований через зовнішні DbContextOptions.
    /// У середовищі без сервера MySQL демонструється через SQLite.
    /// </summary>
    private sealed class ApplicationContext : DbContext
    {
        /// <summary>
        /// Таблиця користувачів у базі даних, через яку виконуються запити та збереження змін.
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Конструктор із зовнішніми опціями дозволяє перемикати провайдер (UseMySql / UseSqlite)
        /// без переписування самого класу ApplicationContext.
        /// </summary>
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        /*
         * Варіант із Metanit для MySQL:
         * Рекомендований NuGet-пакет: Pomelo.EntityFrameworkCore.MySql
         * (Також існує офіційний пакет від Oracle: MySql.EntityFrameworkCore,
         * але Pomelo має кращу підтримку можливостей EF Core та оновлюється швидше.)
         *
         * protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         * {
         *     optionsBuilder.UseMySql(
         *         "server=localhost;user=root;password=123456789;database=usersdb;",
         *         new MySqlServerVersion(new Version(8, 0, 25)));
         * }
         *
         * Особливість Pomelo: другим параметром передається версія сервера MySQL
         * (або ServerVersion.AutoDetect(connectionString), що виконує додатковий запит при старті).
         */
    }

    /// <summary>
    /// Демонструє конфігурацію провайдера (SQLite як замінник MySQL),
    /// збереження сутностей, читання та особливості генерації SQL у MySQL порівняно з SQLite.
    /// </summary>
    public void Run()
    {
        SqliteDbHelper.PrintSection("1. Конфігурація DbContextOptions (SQLite замість MySQL)");

        // Отримуємо шлях до окремого файлу БД SQLite для цього уроку
        var connectionString = SqliteDbHelper.GetConnectionString("ch02_lesson_7_2");
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(connectionString) // У продакшені з MySQL тут викликається .UseMySql(connStr, serverVersion)
            .Options;

        using (var db = new ApplicationContext(options))
        {
            // Перестворюємо схему БД з нуля (EnsureDeleted + EnsureCreated)
            SqliteDbHelper.RecreateDatabase(db);

            Console.WriteLine($"Активний провайдер EF Core: {db.Database.ProviderName}");
            Console.WriteLine("Цільовий провайдер з Metanit: Pomelo.EntityFrameworkCore.MySql (або MySql.EntityFrameworkCore)");
            Console.WriteLine("Метод підключення: optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 25)))");

            var user1 = new User { Name = "Tom", Age = 33 };
            var user2 = new User { Name = "Alice", Age = 26 };

            // Позначаємо нові об'єкти як Added у ChangeTracker
            db.Users.AddRange(user1, user2);
            // Виконуємо INSERT у межах транзакції; SQLite повертає згенеровані Id через RETURNING "Id"
            // (у MySQL провайдер Pomelo використовує LAST_INSERT_ID() для отримання згенерованого ключа)
            db.SaveChanges();
            Console.WriteLine("Об'єкти успішно збережені.");
        }

        SqliteDbHelper.PrintSection("2. Читання даних з БД");

        // Новий контекст із чистим ChangeTracker гарантує реальний SELECT-запит до БД
        using (var db = new ApplicationContext(options))
        {
            // Матеріалізуємо результат запиту в список; отримані сутності відстежуються як Unchanged
            var users = db.Users.ToList();
            Console.WriteLine("Список об'єктів:");
            foreach (var u in users)
            {
                Console.WriteLine($"{u.Id}.{u.Name} - {u.Age}");
            }
        }

        SqliteDbHelper.PrintSection("3. Генерація SQL-запиту (ToQueryString)");

        using (var db = new ApplicationContext(options))
        {
            // Параметризована змінна: у SQL підставиться як параметр @__minAge_0, а не літерал
            int minAge = 20;
            // Комбінуємо фільтрацію (включно з перевіркою NULL -> IS NOT NULL), сортування та обмеження вибірки (LIMIT)
            var query = db.Users
                .Where(u => u.Age >= minAge && u.Name != null)
                .OrderByDescending(u => u.Age)
                .Take(5);

            // ToQueryString() повертає SQL, згенерований активним провайдером без звернення до БД
            Console.WriteLine("Згенерований SQLite SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();
            Console.WriteLine("Примітка: у MySQL (Pomelo) ідентифікатори екрануються зворотними лапками `Users`.`Name`,");
            Console.WriteLine("а автоінкрементний ключ створюється з атрибутом AUTO_INCREMENT.");
        }
    }
}

using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch02;

/// <summary>
/// Урок 7.1: Провайдер MS SQL Server (та адаптація під SQLite).
/// Оригінал: https://metanit.com/sharp/efcore/7.1.php
/// </summary>
public class Lesson_7_1_SqlServerProvider : ILesson
{
    public string Id => "7.1";
    public string Chapter => "02. Провайдери баз даних";
    public string Title => "Провайдер MS SQL Server (та адаптація під SQLite)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/7.1.php";
    public int ChapterOrder => 2;
    public int LessonOrder => 1;

    // Ізольовані моделі та контекст для цього уроку, щоб уникнути конфліктів імен у проєкті.
    /// <summary>
    /// Модель користувача, що відображається на таблицю Users у базі даних.
    /// </summary>
    private sealed class User
    {
        /// <summary>
        /// Первинний ключ. За домовленістю EF Core властивість з іменем Id
        /// автоматично налаштовується як автоінкрементний PK
        /// (IDENTITY(1,1) у MS SQL Server, INTEGER PRIMARY KEY AUTOINCREMENT у SQLite).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача. Nullable-тип (string?) означає, що стовпець у БД допускає NULL
        /// (TEXT NULL у SQLite / NVARCHAR(MAX) NULL у MS SQL Server).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Вік користувача. Значущий тип (int) створює стовпець NOT NULL
        /// (INTEGER NOT NULL у SQLite / INT NOT NULL у MS SQL Server).
        /// </summary>
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст даних, що приймає зовнішні DbContextOptions.
    /// У реальному проєкті з MS SQL Server сюди передається UseSqlServer(...),
    /// а в цьому навчальному середовищі — UseSqlite(...).
    /// </summary>
    private sealed class ApplicationContext : DbContext
    {
        /// <summary>
        /// Набір сутностей Users, який відповідає таблиці "Users" у БД
        /// і слугує точкою входу для LINQ-запитів та операцій додавання/оновлення.
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Ініціалізує контекст із готовими параметрами підключення (провайдер, рядок підключення тощо).
        /// Такий підхід дозволяє легко підміняти провайдер (наприклад, SQL Server на SQLite) без зміни коду контексту.
        /// </summary>
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        /*
         * Варіант із Metanit для MS SQL Server (потрібен NuGet-пакет Microsoft.EntityFrameworkCore.SqlServer):
         *
         * protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         * {
         *     optionsBuilder.UseSqlServer(
         *         @"Server=(localdb)\mssqllocaldb;Database=helloappdb;Trusted_Connection=True;");
         * }
         *
         * Пояснення параметрів рядка підключення MS SQL Server:
         * - Server=(localdb)\mssqllocaldb  — локальний движок SQL Server LocalDB для розробки
         *   (для SQL Server Express зазвичай використовується .\SQLEXPRESS).
         * - Database=helloappdb            — назва бази даних.
         * - Trusted_Connection=True        — автентифікація Windows.
         * - TrustServerCertificate=True    — часто потрібна в сучасних версіях Microsoft.Data.SqlClient.
         */
    }

    /// <summary>
    /// Демонструє налаштування провайдера БД через DbContextOptionsBuilder,
    /// базові CRUD-операції (додавання та читання) і перегляд згенерованого SQL.
    /// </summary>
    public void Run()
    {
        SqliteDbHelper.PrintSection("1. Налаштування DbContextOptions та перевірка провайдера");

        // Формуємо рядок підключення до локального файлу SQLite (Data Source=...).
        // Конфігуруємо DbContextOptions через DbContextOptionsBuilder замість OnConfiguring,
        // щоб не жорстко прив'язувати контекст до конкретного провайдера.
        var connectionString = SqliteDbHelper.GetConnectionString("ch02_lesson_7_1");
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(connectionString) // Реєструє сервіси провайдера Microsoft.EntityFrameworkCore.Sqlite
            .Options;

        // Створюємо (перестворюємо) базу даних та додаємо тестові дані (як у прикладі Metanit)
        using (var db = new ApplicationContext(options))
        {
            // Під капотом викликає EnsureDeleted() + EnsureCreated():
            // видаляє старий файл БД (якщо є) і створює таблицю "Users" напряму за моделлю без міграцій.
            SqliteDbHelper.RecreateDatabase(db);

            // Database.ProviderName повертає ім'я активного NuGet-пакета провайдера
            Console.WriteLine($"Активний провайдер EF Core: {db.Database.ProviderName}");
            Console.WriteLine("Еквівалент для MS SQL Server: Microsoft.EntityFrameworkCore.SqlServer");
            Console.WriteLine("NuGet-пакет для MS SQL Server: Microsoft.EntityFrameworkCore.SqlServer");

            var user1 = new User { Name = "Tom", Age = 33 };
            var user2 = new User { Name = "Alice", Age = 26 };

            // AddRange переводить обидві сутності у стан EntityState.Added у ChangeTracker
            db.Users.AddRange(user1, user2);
            // SaveChanges() відкриває транзакцію, генерує INSERT INTO "Users" ... RETURNING "Id"
            // і автоматично записує згенеровані SQLite значення Id назад у user1.Id та user2.Id
            db.SaveChanges();
        }

        SqliteDbHelper.PrintSection("2. Отримання даних з БД");

        // Новий екземпляр DbContext гарантує порожній ChangeTracker,
        // щоб дані точно завантажувалися з файлу SQLite, а не з кешу попереднього контексту.
        using (var db = new ApplicationContext(options))
        {
            // ToList() матеріалізує LINQ-запит у SQL: SELECT "u"."Id", "u"."Age", "u"."Name" FROM "Users" AS "u"
            // і починає відстежувати завантажені об'єкти у ChangeTracker (стан Unchanged).
            var users = db.Users.ToList();
            Console.WriteLine("Users list:");
            foreach (var u in users)
            {
                Console.WriteLine($"{u.Id}.{u.Name} - {u.Age}");
            }
        }

        SqliteDbHelper.PrintSection("3. Генерація SQL-запиту (ToQueryString)");

        using (var db = new ApplicationContext(options))
        {
            // Локальна змінна захоплюється в дерево виразів і транслюється в SQL-параметр (@__minAge_0),
            // що захищає від SQL-ін'єкцій та дозволяє кешувати план запиту (особливо важливо у MS SQL Server).
            int minAge = 25;
            // Метод ToQueryString() компілює LINQ-вираз у точний SQL для поточного провайдера без виконання запиту до БД
            var query = db.Users
                .Where(u => u.Age > minAge)
                .OrderBy(u => u.Name)
                .Take(10);

            Console.WriteLine("Згенерований SQLite SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();
            Console.WriteLine("Примітка: у MS SQL Server замість подвійних лапок \"Users\" використовуються");
            Console.WriteLine("квадратні дужки [Users], а замість LIMIT @__p_1 — конструкція TOP(@__p_1).");
        }
    }
}

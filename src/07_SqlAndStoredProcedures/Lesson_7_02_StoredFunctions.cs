using EFCoreConsoleMastery.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_2
{
    /// <summary>
    /// Модель компанії для уроку 6.2 (демонстрація збережених функцій та SQLite UDF).
    /// </summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Колекційна навігаційна властивість до працівників компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>
    /// Модель користувача для уроку 6.2.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }

        /// <summary>Зовнішній ключ на таблицю <c>Companies</c>.</summary>
        public int CompanyId { get; set; }

        /// <summary>Посилальна навігаційна властивість на компанію користувача.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Контекст БД із демонстрацією проєкції функцій БД на C#-методи через <c>HasDbFunction</c>.
    /// У SQLite табличні функції (TVF) через <c>CREATE FUNCTION</c> не підтримуються,
    /// натомість можна реєструвати скалярні UDF (User-Defined Functions) через <c>SqliteConnection.CreateFunction</c>
    /// або проєктувати вбудовані функції SQLite (<c>length</c>, <c>upper</c> тощо).
    /// </summary>
    public class ApplicationContext : DbContext
    {
        private readonly SqliteConnection _connection;

        /// <summary>
        /// Ініціалізує контекст із уже створеним і відкритим з'єднанням <see cref="SqliteConnection"/>,
        /// на якому зареєстровані користувацькі SQLite-функції (UDF).
        /// </summary>
        /// <param name="connection">Відкрите з'єднання SQLite із зареєстрованими UDF.</param>
        public ApplicationContext(SqliteConnection connection)
        {
            _connection = connection;
        }

        /// <summary>Таблиця користувачів (<c>Users</c>).</summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>Таблиця компаній (<c>Companies</c>).</summary>
        public DbSet<Company> Companies => Set<Company>();

        /// <summary>
        /// C#-заглушка для проєкції на користувацьку SQLite-функцію <c>is_senior_user(age, threshold)</c>.
        /// Безпосередньо в пам'яті не викликається — під час побудови дерева виразів LINQ
        /// EF Core замінює виклик цього методу на SQL-виклик <c>is_senior_user("u"."Age", @__threshold_0)</c>.
        /// </summary>
        public static bool IsSeniorUser(int age, int threshold)
            => throw new NotSupportedException("Цей метод призначений лише для LINQ-запитів EF Core.");

        /// <summary>
        /// C#-заглушка для проєкції на користувацьку SQLite-функцію <c>format_user_badge(name, age)</c>.
        /// </summary>
        public static string FormatUserBadge(string name, int age)
            => throw new NotSupportedException("Цей метод призначений лише для LINQ-запитів EF Core.");

        /// <summary>
        /// C#-заглушка для проєкції на вбудовану скалярну функцію SQLite <c>length(text)</c>.
        /// </summary>
        public static int SqliteLength(string? text)
            => throw new NotSupportedException("Цей метод призначений лише для LINQ-запитів EF Core.");

        // -------------------------------------------------------------------------
        // ПРИМІТКА ДЛЯ MS SQL SERVER (Metanit 6.2):
        // У SQL Server таблична функція (CREATE FUNCTION [dbo].[GetUsersByAge](@age int)
        // RETURNS TABLE ...) проєктується на метод екземпляра DbContext так:
        //
        // public IQueryable<User> GetUsersByAge(int age)
        //     => FromExpression(() => GetUsersByAge(age));
        //
        // І реєструється в OnModelCreating:
        // modelBuilder.HasDbFunction(() => GetUsersByAge(default));
        //
        // Для скалярної функції SQL Server також можна вказати схему через .HasSchema("dbo").
        // У SQLite схем (dbo тощо) немає, тому використовуємо лише .HasName(...).
        // -------------------------------------------------------------------------

        /// <summary>
        /// Налаштовує EF Core на використання переданого з'єднання <see cref="_connection"/>.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Передаємо вже відкрите SqliteConnection із зареєстрованими UDF.
            // ВАЖЛИВО: Якщо передавати лише рядок підключення, EF Core відкриє нове з'єднання,
            // на якому наші UDF не будуть зареєстровані!
            optionsBuilder.UseSqlite(_connection);
        }

        /// <summary>
        /// Реєструє скалярні функції БД у моделі EF Core через Fluent API (<c>HasDbFunction</c>).
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Прив'язуємо статичний C#-метод до SQL-імені функції.
            // Використання typeof(...).GetMethod(...) з точним переліком типів параметрів
            // однозначно ідентифікує метод для транслятора LINQ-to-SQL.
            modelBuilder.HasDbFunction(
                    typeof(ApplicationContext).GetMethod(nameof(IsSeniorUser), new[] { typeof(int), typeof(int) })!)
                .HasName("is_senior_user");

            modelBuilder.HasDbFunction(
                    typeof(ApplicationContext).GetMethod(nameof(FormatUserBadge), new[] { typeof(string), typeof(int) })!)
                .HasName("format_user_badge");

            modelBuilder.HasDbFunction(
                    typeof(ApplicationContext).GetMethod(nameof(SqliteLength), new[] { typeof(string) })!)
                .HasName("length");
        }
    }
}

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures
{
    using EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_2;

    /// <summary>
    /// Урок 7.2: Збережені функції в EF Core (адаптація для SQLite через UDF + HasDbFunction).
    /// </summary>
    public class Lesson_7_02_StoredFunctions : ILesson
    {
        public string Id => "7.2";
        public string Chapter => "07. SQL в Entity Framework Core";
        public string Title => "Збережені функції (SQLite UDF / HasDbFunction адаптація)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/6.2.php";
        public int ChapterOrder => 7;
        public int LessonOrder => 2;

        /// <summary>
        /// Запускає демонстрацію збережених функцій в EF Core 11, адаптованих під SQLite:
        /// <list type="number">
        /// <item><description>реєстрація C#-делегатів як скалярних функцій SQLite (<c>SqliteConnection.CreateFunction</c>);</description></item>
        /// <item><description>виклик користувацьких та вбудованих функцій у LINQ через <c>HasDbFunction</c>;</description></item>
        /// <item><description>виклик зареєстрованої функції безпосередньо в сирому SQL (<c>FromSqlRaw</c>);</description></item>
        /// <item><description>порівняння з MS SQL Server (<c>CREATE FUNCTION</c> і TVF).</description></item>
        /// </list>
        /// </summary>
        public void Run()
        {
            string dbName = "lesson_6_2_stored_functions";
            string dbPath = SqliteDbHelper.GetDbPath(dbName);
            string connectionString = SqliteDbHelper.GetConnectionString(dbName);

            // Очищаємо пул з'єднань SQLite перед видаленням файлу БД:
            // ADO.NET пулінг тримає дескриптор файлу відкритим навіть після Dispose(),
            // що на Windows призведе до IOException при File.Delete(dbPath).
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
                File.Delete(dbPath);

            // Створюємо та відкриваємо SqliteConnection, щоб зареєструвати користувацькі функції (UDF).
            // У SQLite функції не зберігаються в самому .db-файлі (на відміну від MS SQL Server),
            // а існують у пам'яті поточного з'єднання протягом його часу життя.
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Реєструємо C#-делегати як скалярні SQLite-функції (аналог скалярних збережених функцій).
            // isDeterministic: true повідомляє оптимізатору SQLite, що функція завжди повертає
            // однаковий результат для однакових аргументів (це дозволяє кешувати обчислення в межах запиту).
            connection.CreateFunction<int, int, bool>(
                "is_senior_user",
                (age, threshold) => age >= threshold,
                isDeterministic: true);

            connection.CreateFunction<string, int, string>(
                "format_user_badge",
                (name, age) => $"{name} [{age} р.]",
                isDeterministic: true);

            // 1. Ініціалізація бази даних
            using (var db = new ApplicationContext(connection))
            {
                // Створюємо таблиці, якщо їх ще немає (EnsureDeleted не викликаємо, бо файл щойно перестворено)
                db.Database.EnsureCreated();

                Company microsoft = new() { Name = "Microsoft" };
                Company google = new() { Name = "Google" };
                db.Companies.AddRange(microsoft, google);

                db.Users.AddRange(
                    new User { Name = "Tom", Age = 36, Company = microsoft },
                    new User { Name = "Bob", Age = 39, Company = google },
                    new User { Name = "Alice", Age = 28, Company = microsoft },
                    new User { Name = "Kate", Age = 25, Company = google },
                    new User { Name = "Tomas", Age = 22, Company = microsoft },
                    new User { Name = "Tomek", Age = 42, Company = google }
                );
                db.SaveChanges();
            }

            using (var db = new ApplicationContext(connection))
            {
                // 2. Виклик SQLite UDF через LINQ (HasDbFunction)
                SqliteDbHelper.PrintSection("1. Виклик SQLite UDF через HasDbFunction у LINQ");

                // EF Core транслює цей LINQ-запит у SQL приблизно такого вигляду:
                // SELECT format_user_badge(COALESCE("u"."Name", 'Unknown'), "u"."Age") AS "Badge",
                //        length("u"."Name") AS "NameLength"
                // FROM "Users" AS "u"
                // WHERE is_senior_user("u"."Age", @__threshold_0)
                // ORDER BY "u"."Age"
                // Тобто фільтрація та форматування відбуваються на боці SQLite, а не після ToList() у C#!
                int threshold = 30;
                var seniorBadges = db.Users
                    .Where(u => ApplicationContext.IsSeniorUser(u.Age, threshold))
                    .OrderBy(u => u.Age)
                    .Select(u => new
                    {
                        Badge = ApplicationContext.FormatUserBadge(u.Name ?? "Unknown", u.Age),
                        NameLength = ApplicationContext.SqliteLength(u.Name)
                    })
                    .ToList();

                Console.WriteLine($"Користувачі віком >= {threshold} (обчислено через UDF у SQLite):");
                foreach (var item in seniorBadges)
                    Console.WriteLine($"  - {item.Badge} (довжина імені = {item.NameLength})");

                // 3. Виклик зареєстрованої функції безпосередньо в сирому SQL (FromSqlRaw)
                SqliteDbHelper.PrintSection("2. Виклик функції безпосередньо у FromSqlRaw");

                // Аналогічно до виклику збереженої функції в SQL-запиті на SQL Server.
                // Зверніть увагу на "= 0": у SQLite немає окремого типу BOOLEAN —
                // значення bool із C#-функції повертається як INTEGER (1 для true, 0 для false).
                var youngerUsers = db.Users
                    .FromSqlRaw("SELECT * FROM Users WHERE is_senior_user(Age, {0}) = 0 ORDER BY Age", threshold)
                    .ToList();

                Console.WriteLine($"Користувачі віком < {threshold} (FromSqlRaw + UDF):");
                foreach (var u in youngerUsers)
                    Console.WriteLine($"  - {u.Name} ({u.Age} р.)");

                // 4. Пояснення відмінностей від SQL Server
                SqliteDbHelper.PrintSection("3. Порівняння з MS SQL Server (CREATE FUNCTION)");
                Console.WriteLine("SQLite не підтримує CREATE FUNCTION у DDL (на відміну від T-SQL).");
                Console.WriteLine("Проте Microsoft.Data.Sqlite дозволяє реєструвати C#-функції через CreateFunction(),");
                Console.WriteLine("а EF Core HasDbFunction() однаково працює для обох підходів (UDF та вбудованих функцій).");
            }
        }
    }
}

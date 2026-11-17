using EFCoreConsoleMastery.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_3
{
    /// <summary>
    /// Модель компанії для уроку 6.3 (порівняння збережених процедур MS SQL Server та патернів SQLite).
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
    /// Модель користувача для уроку 6.3.
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
    /// Контекст бази даних SQLite для уроку 6.3.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        private readonly string _connectionString;

        /// <summary>
        /// Ініціалізує контекст із заданим рядком підключення до SQLite.
        /// </summary>
        /// <param name="connectionString">Рядок підключення формату <c>Data Source=...</c>.</param>
        public ApplicationContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>Таблиця користувачів (<c>Users</c>).</summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>Таблиця компаній (<c>Companies</c>).</summary>
        public DbSet<Company> Companies => Set<Company>();

        /// <summary>
        /// Налаштовує провайдер SQLite для цього екземпляра <see cref="DbContext"/>.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Підключаємо провайдер Microsoft.EntityFrameworkCore.Sqlite
            optionsBuilder.UseSqlite(_connectionString);
        }
    }
}

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures
{
    using EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_3;

    /// <summary>
    /// Урок 6.3: Збережені процедури (обмеження SQLite та параметризований SQL у транзакціях).
    /// </summary>
    public class Lesson_6_3_StoredProcedures : ILesson
    {
        public string Id => "6.3";
        public string Chapter => "07. SQL в Entity Framework Core";
        public string Title => "Збережені процедури (обмеження SQLite та параметризований SQL)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/6.3.php";
        public int ChapterOrder => 7;
        public int LessonOrder => 3;

        /// <summary>
        /// Запускає демонстрацію роботи зі збереженими процедурами (обмеження SQLite та еквівалентні патерни):
        /// <list type="number">
        /// <item><description>пояснення обмежень SQLite порівняно з MS SQL Server (<c>CREATE PROCEDURE</c>, <c>EXEC</c>, <c>OUTPUT</c>);</description></item>
        /// <item><description>еквівалент процедури вибірки з параметром через <c>FromSqlRaw</c> + <see cref="SqliteParameter"/>;</description></item>
        /// <item><description>еквівалент <c>OUTPUT</c>-параметра через <c>Database.SqlQuery&lt;string&gt;</c>;</description></item>
        /// <item><description>атомарна багатокрокова DML-операція в явній транзакції (<c>BeginTransaction</c> + <c>ExecuteSql</c>).</description></item>
        /// </list>
        /// </summary>
        public void Run()
        {
            string connectionString = SqliteDbHelper.GetConnectionString("lesson_6_3_stored_procedures");

            // 1. Початкове наповнення бази даних
            using (var db = new ApplicationContext(connectionString))
            {
                // Перестворюємо схему та файл БД для чистого старту уроку
                SqliteDbHelper.RecreateDatabase(db);

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

            // Окремий контекст гарантує, що жодна сутність ще не закешована в ChangeTracker
            using (var db = new ApplicationContext(connectionString))
            {
                SqliteDbHelper.PrintSection("1. Обмеження SQLite щодо збережених процедур");
                Console.WriteLine("УВАГА: SQLite — це вбудована файлова СУБД без серверного рушія процедур.");
                Console.WriteLine("Вона НЕ підтримує CREATE PROCEDURE, EXEC та OUTPUT-параметри.");
                Console.WriteLine("Нижче показано еквівалентні підходи для SQLite через параметризований SQL та транзакції.");

                // -----------------------------------------------------------------
                // ПАТЕРН ДЛЯ MS SQL SERVER (Metanit 6.3 — вибірка за вхідним параметром):
                //
                // CREATE PROCEDURE [dbo].[GetUsersByCompany] @name nvarchar(50) AS
                //     SELECT * FROM Users
                //     WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = @name)
                //
                // У коді C#:
                // SqlParameter param = new("@name", "Microsoft");
                // var users = db.Users.FromSqlRaw("GetUsersByCompany @name", param).ToList();
                //
                // ВАЖЛИВИЙ НЮАНС EF CORE ДЛЯ SQL SERVER:
                // Результат виклику збереженої процедури (EXEC ...) НЕ можна компонувати з LINQ
                // (.Where, .OrderBy, .Include) на боці БД, бо EXEC не можна загорнути в підзапит!
                // Спочатку треба матеріалізувати дані через .ToList() або .AsEnumerable().
                // -----------------------------------------------------------------

                SqliteDbHelper.PrintSection("2. Альтернатива GetUsersByCompany: параметризований FromSqlRaw / FromSqlInterpolated");

                string targetCompany = "Microsoft";
                // SqliteParameter (аналог SqlParameter у SQL Server) передає значення окремо від SQL-тексту,
                // повністю унеможливлюючи SQL-ін'єкції.
                SqliteParameter companyParam = new("@name", targetCompany);

                // На відміну від виклику EXEC у SQL Server, цей SELECT-запит у SQLite є компонованим (composable),
                // тобто за потреби до нього можна додати .OrderBy(...), .Include(...) або .AsNoTracking().
                var companyUsers = db.Users
                    .FromSqlRaw(
                        "SELECT * FROM Users WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = @name)",
                        companyParam)
                    .ToList();

                Console.WriteLine($"Працівники компанії '{targetCompany}' (параметризований FromSqlRaw):");
                foreach (var u in companyUsers)
                    Console.WriteLine($"  - {u.Name} ({u.Age} р.)");

                // -----------------------------------------------------------------
                // ПАТЕРН ДЛЯ MS SQL SERVER (Metanit 6.3 — вихідний параметр OUTPUT):
                //
                // CREATE PROCEDURE [dbo].[GetUserWithMaxAge] @name varchar(50) OUTPUT AS
                //     SELECT @name = [Name] FROM Users WHERE Age = (SELECT MAX(Age) FROM Users)
                //
                // У коді C#:
                // SqlParameter param = new() {
                //     ParameterName = "@userName",
                //     SqlDbType = SqlDbType.VarChar,
                //     Direction = ParameterDirection.Output,
                //     Size = 50
                // };
                // db.Database.ExecuteSqlRaw("GetUserWithMaxAge @userName OUT", param);
                // Console.WriteLine(param.Value);
                // -----------------------------------------------------------------

                SqliteDbHelper.PrintSection("3. Альтернатива OUTPUT-параметру: Database.SqlQuery<string>");

                // У SQLite замість OUTPUT-параметра використовуємо скалярний запит Database.SqlQuery<string>:
                // - стовпець обов'язково називаємо AS Value (вимога EF Core 8+ для скалярів);
                // - LIMIT 1 (аналог TOP 1 у MS SQL Server) гарантує щонайбільше 1 рядок для SingleOrDefault(),
                //   навіть якщо кілька користувачів мають однаковий максимальний вік.
                string? oldestUserName = db.Database
                    .SqlQuery<string>($"""
                        SELECT Name AS Value
                        FROM Users
                        WHERE Age = (SELECT MAX(Age) FROM Users)
                        LIMIT 1
                        """)
                    .SingleOrDefault();

                Console.WriteLine($"Найстарший користувач (через SqlQuery<string>): {oldestUserName}");

                // -----------------------------------------------------------------
                // 4. Атомарна зміна даних (транзакція + ExecuteSql)
                // У клієнт-серверних СУБД кілька DML-кроків часто ховають у збережену процедуру.
                // У SQLite ту саму атомарність ("все або нічого") забезпечує явна транзакція:
                // без BeginTransaction() кожен ExecuteSql виконувався б у режимі auto-commit окремо.
                // -----------------------------------------------------------------
                SqliteDbHelper.PrintSection("4. Атомарна багатокрокова операція в транзакції");

                using var transaction = db.Database.BeginTransaction();
                try
                {
                    string newComp = "OpenAI";
                    // ExecuteSql приймає FormattableString і перетворює {newComp} на безпечний параметр @p0
                    db.Database.ExecuteSql(
                        $"INSERT INTO Companies (Name) VALUES ({newComp})");

                    int ageIncrease = 1;
                    // Другий DML-крок виконується в тій самій транзакції SQLite (зміни ще не видимі іншим з'єднанням)
                    int updatedCount = db.Database.ExecuteSql(
                        $"UPDATE Users SET Age = Age + {ageIncrease} WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = {targetCompany})");

                    // Фіксуємо обидві операції разом (COMMIT TRANSACTION у SQLite)
                    transaction.Commit();
                    Console.WriteLine($"Транзакцію зафіксовано: додано компанію '{newComp}', оновлено вік для {updatedCount} користувачів '{targetCompany}'.");
                }
                catch
                {
                    // У разі будь-якої помилки відкочуємо всі зміни в межах транзакції (ROLLBACK)
                    transaction.Rollback();
                    throw;
                }

                Console.WriteLine("Оновлені працівники Microsoft:");
                // ВАЖЛИВО: ExecuteSql оновлює БД напряму, оминаючи ChangeTracker!
                // Тому використовуємо AsNoTracking(), щоб гарантовано прочитати оновлені значення Age з БД,
                // а не отримати старі екземпляри з кешу ChangeTracker (якби вони вже були завантажені раніше).
                foreach (var u in db.Users
                    .FromSql($"SELECT * FROM Users WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = {targetCompany})")
                    .AsNoTracking()
                    .ToList())
                {
                    Console.WriteLine($"  - {u.Name} ({u.Age} р.)");
                }
            }
        }
    }
}

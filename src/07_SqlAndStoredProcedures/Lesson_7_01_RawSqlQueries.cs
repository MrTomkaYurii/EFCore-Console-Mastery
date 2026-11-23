using EFCoreConsoleMastery.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_1
{
    /// <summary>
    /// Модель компанії для демонстрації сирих SQL-запитів у EF Core.
    /// Відображається на таблицю <c>Companies</c> (зв'язок один-до-багатьох із <see cref="User"/>).
    /// </summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії (у SQLite створюється як INTEGER PRIMARY KEY AUTOINCREMENT).</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії (nullable TEXT у SQLite).</summary>
        public string? Name { get; set; }

        /// <summary>Колекційна навігаційна властивість до працівників компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>
    /// Модель користувача, пов'язаного з компанією зовнішнім ключем <see cref="CompanyId"/>.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача (INTEGER у SQLite).</summary>
        public int Age { get; set; }

        /// <summary>Зовнішній ключ на таблицю <c>Companies</c> (індексується EF Core за замовчуванням).</summary>
        public int CompanyId { get; set; }

        /// <summary>Посилальна навігаційна властивість на компанію користувача.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>
    /// DTO-модель (Data Transfer Object) для демонстрації <c>DbContext.Database.SqlQuery&lt;T&gt;</c>
    /// без реєстрації сутності у <see cref="DbContext"/> (без <c>DbSet&lt;T&gt;</c> і без відстеження змін).
    /// Імена властивостей мають збігатися з аліасами стовпців у SQL-запиті.
    /// </summary>
    public class CompanyStatDto
    {
        /// <summary>Назва компанії (мапиться з <c>c.Name AS CompanyName</c>).</summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Кількість працівників у компанії (мапиться з <c>COUNT(u.Id) AS UserCount</c>).</summary>
        public int UserCount { get; set; }

        /// <summary>Середній вік працівників (мапиться з <c>COALESCE(AVG(u.Age), 0.0) AS AverageAge</c>).</summary>
        public double AverageAge { get; set; }
    }

    /// <summary>
    /// Контекст бази даних SQLite для уроку 6.1.
    /// Керує підключенням і надає доступ до таблиць <see cref="Users"/> та <see cref="Companies"/>.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        private readonly string _connectionString;

        /// <summary>
        /// Ініціалізує контекст із заданим рядком підключення до файлу SQLite.
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
    using EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_1;

    /// <summary>
    /// Урок 7.1: Виконання сирих SQL-запитів у EF Core
    /// (FromSqlRaw, FromSqlInterpolated, SqlQuery, ExecuteSqlRaw / ExecuteSqlInterpolated).
    /// </summary>
    public class Lesson_7_01_RawSqlQueries : ILesson
    {
        public string Id => "7.1";
        public string Chapter => "07. SQL в Entity Framework Core";
        public string Title => "Виконання SQL-запитів (FromSqlRaw, FromSqlInterpolated, SqlQuery, ExecuteSqlRaw)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/6.1.php";
        public int ChapterOrder => 7;
        public int LessonOrder => 1;

        /// <summary>
        /// Запускає демонстрацію виконання сирих SQL-запитів у EF Core 11 поверх SQLite:
        /// <list type="number">
        /// <item><description><c>FromSqlRaw</c> з подальшою композицією LINQ (<c>OrderBy</c>, <c>Include</c>);</description></item>
        /// <item><description>параметризація в <c>FromSqlRaw</c> (<see cref="SqliteParameter"/> та позиційні плейсхолдери <c>{0}</c>);</description></item>
        /// <item><description>безпечна інтерполяція <c>FromSql</c> (<see cref="FormattableString"/>);</description></item>
        /// <item><description><c>Database.SqlQuery&lt;T&gt;</c> для скалярів і довільних DTO без <c>DbSet</c>;</description></item>
        /// <item><description>DML-операції через <c>ExecuteSqlRaw</c> та <c>ExecuteSql</c>.</description></item>
        /// </list>
        /// </summary>
        public void Run()
        {
            string connectionString = SqliteDbHelper.GetConnectionString("lesson_6_1_raw_sql");

            // 1. Початкове наповнення бази даних
            using (var db = new ApplicationContext(connectionString))
            {
                // Видаляє старий файл БД (якщо є) і створює схему таблиць за моделлю (EnsureDeleted + EnsureCreated)
                SqliteDbHelper.RecreateDatabase(db);

                Company microsoft = new() { Name = "Microsoft" };
                Company google = new() { Name = "Google" };
                db.Companies.AddRange(microsoft, google);

                // EF Core автоматично заповнить CompanyId після збереження батьківських записів
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

            // Окремий DbContext гарантує чистий ChangeTracker: усі вибірки реально підуть у SQLite
            using (var db = new ApplicationContext(connectionString))
            {
                // 2. Базовий FromSqlRaw + композиція з LINQ та Include
                SqliteDbHelper.PrintSection("1. FromSqlRaw + OrderBy + Include");

                // ВАЖЛИВО:
                // 1) SQL у FromSqlRaw має повертати ВСІ стовпці сутності (SELECT * або явний перелік усіх колонок моделі),
                //    інакше матеріалізація сутності впаде.
                // 2) Коли запит компонується з LINQ (OrderBy, Where, Include), EF Core загортає ваш SQL у підзапит:
                //    SELECT "c"."Id", "c"."Name" FROM (SELECT * FROM Companies) AS "c" ORDER BY "c"."Name"
                //    Тому всередині FromSqlRaw не повинно бути крапки з комою ';' наприкінці чи некомпонованих конструкцій (наприклад, EXEC).
                // 3) Повернуті сутності автоматично відстежуються ChangeTracker'ом (якщо не додано .AsNoTracking()).
                var companies = db.Companies
                    .FromSqlRaw("SELECT * FROM Companies")
                    .OrderBy(c => c.Name)
                    .ToList();

                Console.WriteLine("Компанії (відсортовано через LINQ):");
                foreach (var c in companies)
                    Console.WriteLine($"  - {c.Name}");

                // Include() поверх FromSqlRaw згенерує LEFT JOIN із таблицею Companies навколо підзапиту Users
                var usersWithCompany = db.Users
                    .FromSqlRaw("SELECT * FROM Users")
                    .Include(u => u.Company)
                    .ToList();

                Console.WriteLine("Користувачі з підвантаженою компанією (Include):");
                foreach (var u in usersWithCompany)
                    Console.WriteLine($"  - {u.Name} ({u.Age} р.) -> {u.Company?.Name}");

                // 3. Параметри у FromSqlRaw (SqliteParameter та позиційні {0})
                SqliteDbHelper.PrintSection("2. Параметри у FromSqlRaw");

                // Явне використання SqliteParameter (для MS SQL Server це був би SqlParameter) захищає від SQL-ін'єкцій:
                // значення передається окремо від тексту SQL-команди як прив'язаний параметр @name.
                // Примітка: у SQLite оператор LIKE за замовчуванням регістронезалежний для ASCII-символів.
                SqliteParameter nameParam = new("@name", "%Tom%");
                var tomUsers = db.Users
                    .FromSqlRaw("SELECT * FROM Users WHERE Name LIKE @name", nameParam)
                    .ToList();

                Console.WriteLine("Пошук за SqliteParameter (@name = '%Tom%'):");
                foreach (var u in tomUsers)
                    Console.WriteLine($"  - {u.Name} ({u.Age})");

                // Позиційні плейсхолдери {0}, {1} у FromSqlRaw — це НЕ string.Format!
                // EF Core автоматично перетворює їх на параметризовані DbParameter (@p0, @p1, ...).
                int minAge = 30;
                var olderUsers = db.Users
                    .FromSqlRaw("SELECT * FROM Users WHERE Age > {0}", minAge)
                    .ToList();

                Console.WriteLine($"Користувачі старше {minAge} років (позиційний параметр {{0}}):");
                foreach (var u in olderUsers)
                    Console.WriteLine($"  - {u.Name} ({u.Age})");

                // 4. FromSql — безпечна інтерполяція рядків (FormattableString; у старіших версіях EF Core — FromSqlInterpolated)
                SqliteDbHelper.PrintSection("3. FromSqlInterpolated");

                string pattern = "%Tom%";
                int ageLimit = 30;
                // Метод FromSql приймає FormattableString, а не звичайний string.
                // Тому інтерпольовані змінні {pattern} і {ageLimit} НЕ склеюються в рядок SQL,
                // а стають параметрами @p0, @p1 на рівні провайдера SQLite!
                // Увага: якщо попередньо зберегти $"..." у змінну типу string і передати в FromSqlRaw,
                // параметризація зникне і виникне ризик SQL-ін'єкції.
                var filteredUsers = db.Users
                    .FromSql($"SELECT * FROM Users WHERE Name LIKE {pattern} AND Age > {ageLimit}")
                    .ToList();

                Console.WriteLine($"Користувачі з '{pattern}' та Age > {ageLimit}:");
                foreach (var u in filteredUsers)
                    Console.WriteLine($"  - {u.Name} ({u.Age})");

                // 5. Database.SqlQuery<T> — вибірка скалярів та довільних DTO без DbSet (доступно з EF Core 8+)
                SqliteDbHelper.PrintSection("4. Database.SqlQuery<T> (скаляри та DTO)");

                // Для скалярного типу (int, string, DateTime тощо) EF Core вимагає,
                // щоб результуючий стовпець називався Value (тому обов'язково пишемо AS Value).
                // Також підтримується подальша композиція LINQ (Single(), Where(), OrderBy() тощо).
                int totalUsers = db.Database
                    .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Users")
                    .Single();
                Console.WriteLine($"Загальна кількість користувачів (SqlQuery<int>): {totalUsers}");

                // Проєкція на довільний DTO-клас (CompanyStatDto), якого немає в DbContext:
                // - сутності DTO не відстежуються ChangeTracker'ом;
                // - у SQLite AVG() повертає NULL для порожньої групи, тому COALESCE(..., 0.0) гарантує тип REAL/double без NullReference/InvalidCast;
                // - імена стовпців (AS CompanyName, UserCount, AverageAge) мають точно відповідати властивостям DTO.
                var stats = db.Database
                    .SqlQuery<CompanyStatDto>($"""
                        SELECT
                            c.Name AS CompanyName,
                            COUNT(u.Id) AS UserCount,
                            COALESCE(AVG(u.Age), 0.0) AS AverageAge
                        FROM Companies c
                        LEFT JOIN Users u ON u.CompanyId = c.Id
                        GROUP BY c.Name
                        ORDER BY UserCount DESC
                        """)
                    .ToList();

                Console.WriteLine("Статистика по компаніях (SqlQuery<CompanyStatDto>):");
                foreach (var s in stats)
                    Console.WriteLine($"  - {s.CompanyName}: {s.UserCount} працівників, сер. вік {s.AverageAge:F1}");

                // 6. ExecuteSqlRaw та ExecuteSql (DML: INSERT / UPDATE / DELETE)
                SqliteDbHelper.PrintSection("5. ExecuteSqlRaw та ExecuteSqlInterpolated (DML)");

                // ExecuteSqlRaw повертає кількість змінених рядків (affected rows).
                // Позиційні параметри {0}, {1} так само перетворюються на DbParameter.
                string newCompany = "Apple";
                int inserted = db.Database.ExecuteSqlRaw(
                    "INSERT INTO Companies (Name) VALUES ({0})", newCompany);
                Console.WriteLine($"INSERT (ExecuteSqlRaw): додано рядків = {inserted}");

                string updatedName = "Apple Inc.";
                int updated = db.Database.ExecuteSqlRaw(
                    "UPDATE Companies SET Name = {0} WHERE Name = {1}", updatedName, newCompany);
                Console.WriteLine($"UPDATE (ExecuteSqlRaw): оновлено рядків = {updated}");

                int deleted = db.Database.ExecuteSqlRaw(
                    "DELETE FROM Companies WHERE Name = {0}", updatedName);
                Console.WriteLine($"DELETE (ExecuteSqlRaw): видалено рядків = {deleted}");

                // ExecuteSql (аналог ExecuteSqlInterpolated) приймає FormattableString і безпечно параметризує {jetbrains}.
                // ВАЖЛИВО: ExecuteSql / ExecuteSqlRaw виконуються напряму в БД в обхід ChangeTracker!
                // Якщо сутності вже були завантажені в поточний DbContext із відстеженням,
                // їхній стан у пам'яті НЕ оновиться автоматично після ExecuteSql*.
                string jetbrains = "JetBrains";
                int interpolatedInserted = db.Database.ExecuteSql(
                    $"INSERT INTO Companies (Name) VALUES ({jetbrains})");
                Console.WriteLine($"INSERT (ExecuteSql / інтерполяція): додано рядків = {interpolatedInserted}");

                Console.WriteLine("Актуальний список компаній у БД:");
                // AsNoTracking() вимикає відстеження і гарантує отримання свіжих даних із SQLite без кешу ChangeTracker
                foreach (var c in db.Companies.AsNoTracking().ToList())
                    Console.WriteLine($"  - [{c.Id}] {c.Name}");
            }
        }
    }
}

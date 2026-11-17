using EFCoreConsoleMastery.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_1
{
    /// <summary>Модель компанії для демонстрації SQL-запитів.</summary>
    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Модель користувача, пов'язаного з компанією.</summary>
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    /// <summary>DTO-модель для демонстрації DbContext.Database.SqlQuery&lt;T&gt; без прив'язки до DbSet.</summary>
    public class CompanyStatDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public double AverageAge { get; set; }
    }

    /// <summary>Контекст бази даних SQLite для уроку 6.1.</summary>
    public class ApplicationContext : DbContext
    {
        private readonly string _connectionString;

        public ApplicationContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Company> Companies => Set<Company>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }
    }
}

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures
{
    using EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_1;

    /// <summary>
    /// Урок 6.1: Виконання сирих SQL-запитів у EF Core
    /// (FromSqlRaw, FromSqlInterpolated, SqlQuery, ExecuteSqlRaw / ExecuteSqlInterpolated).
    /// </summary>
    public class Lesson_6_1_RawSqlQueries : ILesson
    {
        public string Id => "6.1";
        public string Chapter => "07. SQL в Entity Framework Core";
        public string Title => "Виконання SQL-запитів (FromSqlRaw, FromSqlInterpolated, SqlQuery, ExecuteSqlRaw)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/6.1.php";
        public int ChapterOrder => 7;
        public int LessonOrder => 1;

        public void Run()
        {
            string connectionString = SqliteDbHelper.GetConnectionString("lesson_6_1_raw_sql");

            // 1. Початкове наповнення бази даних
            using (var db = new ApplicationContext(connectionString))
            {
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

            using (var db = new ApplicationContext(connectionString))
            {
                // 2. Базовий FromSqlRaw + композиція з LINQ та Include
                SqliteDbHelper.PrintSection("1. FromSqlRaw + OrderBy + Include");

                // SQL-вираз у FromSqlRaw не повинен сам тягнути зв'язані таблиці,
                // але ми можемо докомпонувати LINQ-оператори (OrderBy, Where, Include).
                var companies = db.Companies
                    .FromSqlRaw("SELECT * FROM Companies")
                    .OrderBy(c => c.Name)
                    .ToList();

                Console.WriteLine("Компанії (відсортовано через LINQ):");
                foreach (var c in companies)
                    Console.WriteLine($"  - {c.Name}");

                var usersWithCompany = db.Users
                    .FromSqlRaw("SELECT * FROM Users")
                    .Include(u => u.Company)
                    .ToList();

                Console.WriteLine("Користувачі з підвантаженою компанією (Include):");
                foreach (var u in usersWithCompany)
                    Console.WriteLine($"  - {u.Name} ({u.Age} р.) -> {u.Company?.Name}");

                // 3. Параметри у FromSqlRaw (SqliteParameter та позиційні {0})
                SqliteDbHelper.PrintSection("2. Параметри у FromSqlRaw");

                // Використання SqliteParameter захищає від SQL-ін'єкцій
                SqliteParameter nameParam = new("@name", "%Tom%");
                var tomUsers = db.Users
                    .FromSqlRaw("SELECT * FROM Users WHERE Name LIKE @name", nameParam)
                    .ToList();

                Console.WriteLine("Пошук за SqliteParameter (@name = '%Tom%'):");
                foreach (var u in tomUsers)
                    Console.WriteLine($"  - {u.Name} ({u.Age})");

                // Позиційні параметри {0}, {1} також автоматично перетворюються на DbParameter
                int minAge = 30;
                var olderUsers = db.Users
                    .FromSqlRaw("SELECT * FROM Users WHERE Age > {0}", minAge)
                    .ToList();

                Console.WriteLine($"Користувачі старше {minAge} років (позиційний параметр {{0}}):");
                foreach (var u in olderUsers)
                    Console.WriteLine($"  - {u.Name} ({u.Age})");

                // 4. FromSqlInterpolated — безпечна інтерполяція рядків (FormattableString)
                SqliteDbHelper.PrintSection("3. FromSqlInterpolated");

                string pattern = "%Tom%";
                int ageLimit = 30;
                // Інтерпольований рядок у FromSqlInterpolated НЕ склеює SQL напряму,
                // а створює параметри @p0, @p1 на рівні провайдера БД!
                var filteredUsers = db.Users
                    .FromSql($"SELECT * FROM Users WHERE Name LIKE {pattern} AND Age > {ageLimit}")
                    .ToList();

                Console.WriteLine($"Користувачі з '{pattern}' та Age > {ageLimit}:");
                foreach (var u in filteredUsers)
                    Console.WriteLine($"  - {u.Name} ({u.Age})");

                // 5. Database.SqlQuery<T> — вибірка скалярів та довільних DTO без DbSet
                SqliteDbHelper.PrintSection("4. Database.SqlQuery<T> (скаляри та DTO)");

                // Для скалярного типу у EF Core 8+ стовпець має називатися Value (або AS Value)
                int totalUsers = db.Database
                    .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Users")
                    .Single();
                Console.WriteLine($"Загальна кількість користувачів (SqlQuery<int>): {totalUsers}");

                // Проєкція на довільний DTO-клас, якого немає в DbContext
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

                // 6. ExecuteSqlRaw та ExecuteSqlInterpolated (INSERT / UPDATE / DELETE)
                SqliteDbHelper.PrintSection("5. ExecuteSqlRaw та ExecuteSqlInterpolated (DML)");

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

                string jetbrains = "JetBrains";
                int interpolatedInserted = db.Database.ExecuteSql(
                    $"INSERT INTO Companies (Name) VALUES ({jetbrains})");
                Console.WriteLine($"INSERT (ExecuteSql / інтерполяція): додано рядків = {interpolatedInserted}");

                Console.WriteLine("Актуальний список компаній у БД:");
                foreach (var c in db.Companies.AsNoTracking().ToList())
                    Console.WriteLine($"  - [{c.Id}] {c.Name}");
            }
        }
    }
}

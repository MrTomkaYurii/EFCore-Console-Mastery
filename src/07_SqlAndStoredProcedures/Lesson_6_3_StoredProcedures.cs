using EFCoreConsoleMastery.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics._07_SqlAndStoredProcedures.L6_3
{
    /// <summary>Модель компанії для уроку 6.3.</summary>
    public class Company
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Модель користувача для уроку 6.3.</summary>
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }

    /// <summary>Контекст бази даних SQLite для уроку 6.3.</summary>
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

        public void Run()
        {
            string connectionString = SqliteDbHelper.GetConnectionString("lesson_6_3_stored_procedures");

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
                // -----------------------------------------------------------------

                SqliteDbHelper.PrintSection("2. Альтернатива GetUsersByCompany: параметризований FromSqlRaw / FromSqlInterpolated");

                string targetCompany = "Microsoft";
                SqliteParameter companyParam = new("@name", targetCompany);

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
                // 4. Атомарна зміна даних (транзакція + ExecuteSqlInterpolated)
                // У клієнт-серверних СУБД кілька DML-кроків часто ховають у збережену процедуру.
                // У SQLite ту саму атомарність забезпечує явна транзакція.
                // -----------------------------------------------------------------
                SqliteDbHelper.PrintSection("4. Атомарна багатокрокова операція в транзакції");

                using var transaction = db.Database.BeginTransaction();
                try
                {
                    string newComp = "OpenAI";
                    db.Database.ExecuteSql(
                        $"INSERT INTO Companies (Name) VALUES ({newComp})");

                    int ageIncrease = 1;
                    int updatedCount = db.Database.ExecuteSql(
                        $"UPDATE Users SET Age = Age + {ageIncrease} WHERE CompanyId = (SELECT Id FROM Companies WHERE Name = {targetCompany})");

                    transaction.Commit();
                    Console.WriteLine($"Транзакцію зафіксовано: додано компанію '{newComp}', оновлено вік для {updatedCount} користувачів '{targetCompany}'.");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                Console.WriteLine("Оновлені працівники Microsoft:");
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

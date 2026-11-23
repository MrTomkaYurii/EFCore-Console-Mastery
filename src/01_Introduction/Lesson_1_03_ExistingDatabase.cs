using EFCoreConsoleMastery.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01;

/// <summary>
/// Урок 1.3. Підключення до існуючої бази даних (Database First).
/// Демонструє створення таблиці в MS SQL Server через сирий T-SQL (без EF Core)
/// та подальше підключення EF Core DbContext до вже існуючої таблиці.
/// </summary>
public class Lesson_1_03_ExistingDatabase : ILesson
{
    public string Id => "1.3";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Підключення до існуючої бази даних";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.3.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 3;

    private sealed class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class ExistingDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l1_03");
        }
    }

    public void Run()
    {
        var connStr = SqlServerDbHelper.ConnectionString;
        SqlServerDbHelper.DropSchemaObjects(connStr, "l1_03");

        SqlServerDbHelper.PrintSection("1. Створення існуючої таблиці через сирий T-SQL (без EF Core)");
        using (var connection = new SqlConnection(connStr))
        {
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE [l1_03].[Users] (
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Name] NVARCHAR(100) NULL,
                    [Age] INT NOT NULL
                );
                INSERT INTO [l1_03].[Users] ([Name], [Age]) VALUES (N'Tom', 33), (N'Alice', 26);
                """;
            cmd.ExecuteNonQuery();
            Console.WriteLine("Таблицю [l1_03].[Users] створено й наповнено сирим T-SQL (без EF Core).");
        }

        SqlServerDbHelper.PrintSection("2. Підключення EF Core DbContext до існуючої таблиці");
        using (var db = new ExistingDbContext())
        {
            // EnsureCreated() НЕ викликаємо — таблиця вже існує в MS SQL Server!
            var users = db.Users.AsNoTracking().ToList();
            foreach (var u in users)
                Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");
        }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch05
{

/// <summary>
/// Урок 4.2: Підхід TPT (Table Per Type — окрема таблиця для кожного типу ієрархії).
/// Властивості базового класу зберігаються в базовій таблиці, а специфічні властивості
/// похідних класів — у власних таблицях, пов'язаних зовнішнім ключем (1-до-1).
/// </summary>
public class Lesson_4_2_TptInheritance : ILesson
{
    public string Id => "4.2";
    public string Chapter => "05. Спадкування";
    public string Title => "Підхід TPT - Table Per Type";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/4.2.php";
    public int ChapterOrder => 5;
    public int LessonOrder => 2;

    public void Run()
    {
        using var db = new L4_2.TptContext();
        SqliteDbHelper.RecreateDatabase(db);

        // 1. Виводимо реальну схему 3 таблиць SQLite з sqlite_master
        SqliteDbHelper.PrintSection("1. Схема таблиць у SQLite (sqlite_master)");
        PrintSqliteSchemas(db);

        // 2. Додаємо дані різних типів ієрархії
        SqliteDbHelper.PrintSection("2. Додавання даних у нормалізовані таблиці TPT");
        var user1 = new L4_2.User { Name = "Tom" };
        var user2 = new L4_2.User { Name = "Bob" };
        var employee = new L4_2.Employee { Name = "Sam", Salary = 500 };
        var manager = new L4_2.Manager { Name = "Robert", Departament = "IT" };

        db.Users.AddRange(user1, user2);
        db.Employees.Add(employee);
        db.Managers.Add(manager);
        db.SaveChanges();
        Console.WriteLine("Додано: 2 User, 1 Employee (Users + Employees), 1 Manager (Users + Managers).");

        // 3. Демонструємо згенерований SQL через ToQueryString()
        SqliteDbHelper.PrintSection("3. Згенерований SQL (ToQueryString)");
        Console.WriteLine("--- Запит db.Users (LEFT JOIN усіх таблиць похідних типів) ---");
        Console.WriteLine(db.Users.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Employees (INNER JOIN з базовою таблицею Users) ---");
        Console.WriteLine(db.Employees.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Managers (INNER JOIN з базовою таблицею Users) ---");
        Console.WriteLine(db.Managers.ToQueryString());

        // 4. Вибірка даних
        SqliteDbHelper.PrintSection("4. Результати вибірки");
        Console.WriteLine("Усі користувачі (db.Users):");
        foreach (var u in db.Users.AsNoTracking().ToList())
        {
            Console.WriteLine($"  Id={u.Id}, Name={u.Name}, Тип={u.GetType().Name}");
        }

        Console.WriteLine("\nТільки працівники (db.Employees):");
        foreach (var emp in db.Employees.AsNoTracking().ToList())
        {
            Console.WriteLine($"  Id={emp.Id}, Name={emp.Name}, Salary={emp.Salary}");
        }

        Console.WriteLine("\nТільки менеджери (db.Managers):");
        foreach (var man in db.Managers.AsNoTracking().ToList())
        {
            Console.WriteLine($"  Id={man.Id}, Name={man.Name}, Departament={man.Departament}");
        }
    }

    private static void PrintSqliteSchemas(DbContext db)
    {
        DbConnection conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            conn.Open();
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT name, sql
            FROM sqlite_master
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name;
            """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine($"Таблиця: {reader.GetString(0)}");
            Console.WriteLine(reader.GetString(1));
            Console.WriteLine();
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch05.L4_2
{
    /// <summary>
    /// Базовий клас ієрархії. Зберігається у таблиці Users.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>
    /// Похідний клас працівника.
    /// Атрибут [Table("Employees")] або Fluent API ToTable("Employees") / UseTptMappingStrategy()
    /// вказують EF Core створити окрему таблицю зі спільним PK/FK Id -> Users(Id).
    /// </summary>
    [Table("Employees")]
    public class Employee : User
    {
        public int Salary { get; set; }
    }

    /// <summary>
    /// Похідний клас менеджера. Зберігається у таблиці Managers (PK/FK Id -> Users(Id)).
    /// </summary>
    [Table("Managers")]
    public class Manager : User
    {
        public string? Departament { get; set; }
    }

    /// <summary>
    /// Контекст даних, що демонструє стратегію TPT.
    /// </summary>
    public class TptContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Manager> Managers { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("lesson_4_2_tpt"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // У EF Core 7+ стратегію TPT можна задати одним викликом для базової сутності:
            modelBuilder.Entity<User>().UseTptMappingStrategy();

            // Альтернативні способи (еквівалентні результати):
            // 1) Атрибут [Table("Employees")] / [Table("Managers")] на похідних класах
            // 2) modelBuilder.Entity<Employee>().ToTable("Employees");
            //    modelBuilder.Entity<Manager>().ToTable("Managers");
        }
    }
}

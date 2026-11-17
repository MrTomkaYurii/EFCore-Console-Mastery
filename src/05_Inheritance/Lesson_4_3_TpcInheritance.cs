using System.Data.Common;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch05
{

/// <summary>
/// Урок 4.3: Підхід TPC (Table Per Concrete Type / Table Per Class — окрема таблиця для кожного конкретного класу).
/// Кожна таблиця містить усі стовпці, включно з успадкованими, що усуває JOIN при вибірці конкретного типу
/// та замінює JOIN на UNION ALL при поліморфних запитах.
/// </summary>
public class Lesson_4_3_TpcInheritance : ILesson
{
    public string Id => "4.3";
    public string Chapter => "05. Спадкування";
    public string Title => "Підхід TPC - Table Per Class";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/4.3.php";
    public int ChapterOrder => 5;
    public int LessonOrder => 3;

    public void Run()
    {
        using var db = new L4_3.TpcContext();
        SqliteDbHelper.RecreateDatabase(db);

        // 1. Виводимо реальну схему 3 незалежних таблиць SQLite з sqlite_master
        SqliteDbHelper.PrintSection("1. Схема таблиць у SQLite (sqlite_master)");
        PrintSqliteSchemas(db);

        // 2. Додаємо дані (унікальні GUID-ключі запобігають колізіям Id між таблицями)
        SqliteDbHelper.PrintSection("2. Додавання даних у незалежні таблиці TPC");
        var user1 = new L4_3.User { Name = "Tom" };
        var user2 = new L4_3.User { Name = "Bob" };
        var employee = new L4_3.Employee { Name = "Sam", Salary = 500 };
        var manager = new L4_3.Manager { Name = "Robert", Departament = "IT" };

        db.Users.AddRange(user1, user2);
        db.Employees.Add(employee);
        db.Managers.Add(manager);
        db.SaveChanges();
        Console.WriteLine("Додано: 2 User у Users, 1 Employee у Employees, 1 Manager у Managers.");

        // 3. Демонструємо згенерований SQL через ToQueryString()
        SqliteDbHelper.PrintSection("3. Згенерований SQL (ToQueryString)");
        Console.WriteLine("--- Запит db.Users (об'єднання таблиць через UNION ALL) ---");
        Console.WriteLine(db.Users.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Employees (прямий SELECT з однієї таблиці без JOIN) ---");
        Console.WriteLine(db.Employees.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Managers (прямий SELECT з однієї таблиці без JOIN) ---");
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

namespace EFCoreConsoleMastery.Topics.Ch05.L4_3
{
    /// <summary>
    /// Базовий клас ієрархії для TPC.
    /// Оскільки SQLite не підтримує SEQUENCE (послідовності БД), як у SQL Server,
    /// використовуємо строковий GUID-ідентифікатор, що гарантує унікальність Id
    /// між різними таблицями ієрархії.
    /// </summary>
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? Name { get; set; }
    }

    /// <summary>
    /// Похідний клас працівника. Таблиця Employees міститиме Id, Name та Salary.
    /// </summary>
    public class Employee : User
    {
        public int Salary { get; set; }
    }

    /// <summary>
    /// Похідний клас менеджера. Таблиця Managers міститиме Id, Name та Departament.
    /// </summary>
    public class Manager : User
    {
        public string? Departament { get; set; }
    }

    /// <summary>
    /// Контекст даних, що демонструє стратегію TPC.
    /// </summary>
    public class TpcContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Manager> Managers { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("lesson_4_3_tpc"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Вмикаємо стратегію TPC (Table Per Concrete Type), доступну з EF Core 7.0
            modelBuilder.Entity<User>().UseTpcMappingStrategy();
        }
    }
}

using System.Data.Common;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch05
{

/// <summary>
/// Урок 4.1: Підхід TPH (Table Per Hierarchy — одна таблиця на всю іерархію класів).
/// Стратегія за замовчуванням у EF Core, яка зберігає всі класи ієрархії в одній таблиці
/// зі службовим стовпцем-дискримінатором (Discriminator).
/// </summary>
public class Lesson_4_1_TphInheritance : ILesson
{
    public string Id => "4.1";
    public string Chapter => "05. Спадкування";
    public string Title => "Підхід TPH - Table Per Hierarchy";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/4.1.php";
    public int ChapterOrder => 5;
    public int LessonOrder => 1;

    public void Run()
    {
        using var db = new L4_1.TphContext();
        SqliteDbHelper.RecreateDatabase(db);

        // 1. Виводимо реальну схему таблиць SQLite з sqlite_master
        SqliteDbHelper.PrintSection("1. Схема таблиць у SQLite (sqlite_master)");
        PrintSqliteSchemas(db);

        // 2. Додаємо екземпляри базового та похідних класів
        SqliteDbHelper.PrintSection("2. Додавання даних різних типів ієрархії");
        var user1 = new L4_1.User { Name = "Tom" };
        var user2 = new L4_1.User { Name = "Bob" };
        var employee = new L4_1.Employee { Name = "Sam", Salary = 500 };
        var manager = new L4_1.Manager { Name = "Robert", Departament = "IT" };

        db.Users.AddRange(user1, user2);
        db.Employees.Add(employee);
        db.Managers.Add(manager);
        db.SaveChanges();
        Console.WriteLine("Додано: 2 User, 1 Employee, 1 Manager в одну таблицю Users.");

        // 3. Демонструємо згенерований SQL через ToQueryString()
        SqliteDbHelper.PrintSection("3. Згенерований SQL (ToQueryString)");
        Console.WriteLine("--- Запит db.Users ---");
        Console.WriteLine(db.Users.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Employees (фільтр за Discriminator) ---");
        Console.WriteLine(db.Employees.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Managers (фільтр за Discriminator) ---");
        Console.WriteLine(db.Managers.ToQueryString());

        // 4. Вибірка та виведення результатів (включаючи значення Discriminator)
        SqliteDbHelper.PrintSection("4. Результати вибірки");
        Console.WriteLine("Усі користувачі (db.Users):");
        foreach (var u in db.Users.AsNoTracking().ToList())
        {
            // Значення властивості Discriminator доступне для читання
            Console.WriteLine($"  Id={u.Id}, Name={u.Name}, Тип={u.GetType().Name}, Discriminator={u.Discriminator}");
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

    /// <summary>
    /// Читає DDL-схему всіх таблиць користувача з системної таблиці sqlite_master.
    /// </summary>
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

namespace EFCoreConsoleMastery.Topics.Ch05.L4_1
{
    /// <summary>
    /// Базовий клас ієрархії користувачів.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        /// <summary>
        /// Необов'язкова властивість, що відображається на службовий стовпець Discriminator.
        /// На рівні БД значення встановлюється автоматично і є доступним лише для читання.
        /// </summary>
        public string? Discriminator { get; set; }
    }

    /// <summary>
    /// Похідний клас працівника. У таблиці Users стовпець Salary буде nullable,
    /// оскільки не всі рядки належать до типу Employee.
    /// </summary>
    public class Employee : User
    {
        public int Salary { get; set; }
    }

    /// <summary>
    /// Похідний клас менеджера. У таблиці Users стовпець Departament буде nullable.
    /// </summary>
    public class Manager : User
    {
        public string? Departament { get; set; }
    }

    /// <summary>
    /// Контекст даних, що демонструє стратегію TPH.
    /// </summary>
    public class TphContext : DbContext
    {
        // Для включення всіх класів ієрархії в модель визначаємо DbSet для кожного типу
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Manager> Managers { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("lesson_4_1_tph"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TPH використовується за замовчуванням, але з EF Core 7+ його можна вказати явно:
            modelBuilder.Entity<User>().UseTphMappingStrategy();
        }
    }
}

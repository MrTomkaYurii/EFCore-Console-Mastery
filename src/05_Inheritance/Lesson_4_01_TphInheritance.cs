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
public class Lesson_4_01_TphInheritance : ILesson
{
    public string Id => "4.1";
    public string Chapter => "05. Спадкування";
    public string Title => "Підхід TPH - Table Per Hierarchy";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/4.1.php";
    public int ChapterOrder => 5;
    public int LessonOrder => 1;

    /// <summary>
    /// Запускає демонстрацію стратегії спадкування TPH (Table Per Hierarchy):
    /// перевіряє DDL-схему в SQLite, додає об'єкти різних типів ієрархії в одну таблицю,
    /// показує згенерований SQL із фільтрацією за дискримінатором та виконує поліморфні запити.
    /// </summary>
    public void Run()
    {
        using var db = new L4_1.TphContext();
        // Перестворюємо базу даних з нуля (EnsureDeleted + EnsureCreated), щоб гарантувати чистий стан без міграцій.
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

        // EF Core автоматично визначить CLR-тип кожного об'єкта і запише відповідне значення
        // у службовий стовпець Discriminator ("User", "Employee", "Manager").
        // Для user1/user2 стовпці Salary та Departament у таблиці Users отримають значення NULL.
        db.Users.AddRange(user1, user2);
        db.Employees.Add(employee);
        db.Managers.Add(manager);
        // SaveChanges() виконає пакетний INSERT в одну таблицю "Users" у межах однієї транзакції SQLite.
        db.SaveChanges();
        Console.WriteLine("Додано: 2 User, 1 Employee, 1 Manager в одну таблицю Users.");

        // 3. Демонструємо згенерований SQL через ToQueryString()
        SqliteDbHelper.PrintSection("3. Згенерований SQL (ToQueryString)");
        Console.WriteLine("--- Запит db.Users ---");
        // ToQueryString() формує SQL-запит без його фактичного виконання в БД.
        // Для базового типу User фільтр за Discriminator не потрібен (якщо всі рядки належать до ієрархії User).
        Console.WriteLine(db.Users.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Employees (фільтр за Discriminator) ---");
        // Для похідного типу EF Core автоматично додає умову WHERE "u"."Discriminator" = 'Employee'.
        Console.WriteLine(db.Employees.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Managers (фільтр за Discriminator) ---");
        // Аналогічно для Manager: WHERE "u"."Discriminator" = 'Manager'.
        Console.WriteLine(db.Managers.ToQueryString());

        // 4. Вибірка та виведення результатів (включаючи значення Discriminator)
        SqliteDbHelper.PrintSection("4. Результати вибірки");
        Console.WriteLine("Усі користувачі (db.Users):");
        // AsNoTracking() вимикає відстеження змін (Change Tracker) для підвищення продуктивності при читанні.
        // Попри це, EF Core все одно матеріалізує кожен рядок у точний похідний CLR-тип (User, Employee або Manager)
        // на основі значення стовпця Discriminator.
        foreach (var u in db.Users.AsNoTracking().ToList())
        {
            // Значення властивості Discriminator доступне для читання, оскільки ми явно оголосили її в моделі User.
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
    /// Читає DDL-схему всіх таблиць користувача з системної таблиці <c>sqlite_master</c>,
    /// щоб наочно показати, як EF Core згенерував структуру таблиці в SQLite.
    /// </summary>
    /// <param name="db">Активний контекст бази даних EF Core.</param>
    private static void PrintSqliteSchemas(DbContext db)
    {
        // Отримуємо низькорівневе ADO.NET-з'єднання, яке використовує EF Core.
        DbConnection conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            conn.Open();
        }

        using var cmd = conn.CreateCommand();
        // Таблиця sqlite_master (у нових версіях також sqlite_schema) містить SQL-визначення всіх об'єктів БД.
        // Фільтр name NOT LIKE 'sqlite_%' виключає внутрішні службові таблиці (наприклад, sqlite_sequence).
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
    /// У стратегії TPH усі класи ієрархії відображаються на одну спільну таблицю <c>Users</c>.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Первинний ключ сутності. У SQLite створюється як <c>INTEGER PRIMARY KEY AUTOINCREMENT</c>.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача (nullable TEXT у SQLite).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Необов'язкова властивість, що відображається на службовий стовпець <c>Discriminator</c>.
        /// На рівні БД значення встановлюється автоматично з імені типу (наприклад, "Employee")
        /// і за замовчуванням є доступним лише для читання (Shadow Property, якщо не оголошено в класі).
        /// </summary>
        public string? Discriminator { get; set; }
    }

    /// <summary>
    /// Похідний клас працівника. У таблиці <c>Users</c> стовпець <c>Salary</c> буде nullable (<c>INTEGER NULL</c>),
    /// навіть попри те, що в C# тип <see cref="int"/> не є nullable — інакше неможливо було б зберігати рядки типу <see cref="User"/> або <see cref="Manager"/>.
    /// </summary>
    public class Employee : User
    {
        /// <summary>
        /// Заробітна плата працівника.
        /// </summary>
        public int Salary { get; set; }
    }

    /// <summary>
    /// Похідний клас менеджера. У таблиці <c>Users</c> стовпець <c>Departament</c> буде nullable (<c>TEXT NULL</c>).
    /// </summary>
    public class Manager : User
    {
        /// <summary>
        /// Назва відділу, яким керує менеджер.
        /// </summary>
        public string? Departament { get; set; }
    }

    /// <summary>
    /// Контекст даних, що демонструє стратегію TPH (Table Per Hierarchy).
    /// </summary>
    public class TphContext : DbContext
    {
        // Для включення всіх класів ієрархії в модель визначаємо DbSet для кожного типу.
        // Навіть без DbSet<Employee> / DbSet<Manager> EF Core може знайти похідні типи, якщо вони згадані в навігаціях,
        // але явні DbSet дають зручний доступ до типізованих запитів (db.Employees, db.Managers).

        /// <summary>
        /// Набір усіх користувачів (поліморфний доступ до всієї ієрархії).
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Набір працівників (автоматично фільтрується за <c>Discriminator = 'Employee'</c>).
        /// </summary>
        public DbSet<Employee> Employees { get; set; } = null!;

        /// <summary>
        /// Набір менеджерів (автоматично фільтрується за <c>Discriminator = 'Manager'</c>).
        /// </summary>
        public DbSet<Manager> Managers { get; set; } = null!;

        /// <summary>
        /// Налаштовує підключення до локальної бази даних SQLite.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("lesson_4_1_tph"));
        }

        /// <summary>
        /// Конфігурує модель даних за допомогою Fluent API.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TPH використовується за замовчуванням у EF Core, але з EF Core 7+ стратегію можна вказати явно.
            // Примітка щодо відмінностей від MS SQL Server: у SQL Server стовпець Discriminator створюється як nvarchar(max)
            // (або обмежений через HasMaxLength), а в SQLite — як TEXT NOT NULL.
            // Головний мінус TPH: неможливість задати NOT NULL обмеження на рівні БД для специфічних полів похідних класів.
            modelBuilder.Entity<User>().UseTphMappingStrategy();
        }
    }
}

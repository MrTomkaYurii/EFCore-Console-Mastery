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

    /// <summary>
    /// Запускає демонстрацію стратегії спадкування TPT (Table Per Type):
    /// виводить DDL-схему трьох нормалізованих таблиць у SQLite, додає об'єкти різних типів,
    /// показує згенерований SQL із <c>LEFT JOIN</c> та <c>INNER JOIN</c>, і виконує поліморфну вибірку.
    /// </summary>
    public void Run()
    {
        using var db = new L4_2.TptContext();
        // Перестворюємо базу даних з нуля (EnsureDeleted + EnsureCreated) для чистого старту без міграцій.
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

        // У TPT кожен екземпляр похідного типу розкладається на кілька таблиць:
        // - user1, user2 -> 1 INSERT у таблицю Users;
        // - employee -> спочатку INSERT у Users (отримуємо згенерований Id), потім INSERT у Employees з тим самим Id;
        // - manager -> спочатку INSERT у Users, потім INSERT у Managers з тим самим Id.
        db.Users.AddRange(user1, user2);
        db.Employees.Add(employee);
        db.Managers.Add(manager);
        // SaveChanges() виконує всі ці INSERT-и в межах однієї транзакції SQLite.
        db.SaveChanges();
        Console.WriteLine("Додано: 2 User, 1 Employee (Users + Employees), 1 Manager (Users + Managers).");

        // 3. Демонструємо згенерований SQL через ToQueryString()
        SqliteDbHelper.PrintSection("3. Згенерований SQL (ToQueryString)");
        Console.WriteLine("--- Запит db.Users (LEFT JOIN усіх таблиць похідних типів) ---");
        // При поліморфному запиті до базового типу EF Core об'єднує Users з усіма таблицями-нащадками через LEFT JOIN
        // і формує обчислюваний вираз CASE WHEN ... IS NOT NULL THEN 'Employee' ..., щоб визначити реальний CLR-тип.
        // У БД стовпця Discriminator немає — тип визначається наявністю рядка у відповідній дочірній таблиці.
        Console.WriteLine(db.Users.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Employees (INNER JOIN з базовою таблицею Users) ---");
        // Запит до конкретного похідного типу використовує INNER JOIN між Employees та Users,
        // оскільки кожен працівник гарантовано має запис у базовій таблиці Users.
        Console.WriteLine(db.Employees.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Managers (INNER JOIN з базовою таблицею Users) ---");
        Console.WriteLine(db.Managers.ToQueryString());

        // 4. Вибірка даних
        SqliteDbHelper.PrintSection("4. Результати вибірки");
        Console.WriteLine("Усі користувачі (db.Users):");
        // AsNoTracking() вимикає Change Tracker (не створює знімки сутностей), що прискорює читання.
        // EF Core все одно матеріалізує правильні похідні типи (User, Employee, Manager) за результатами LEFT JOIN.
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

    /// <summary>
    /// Читає DDL-схему всіх таблиць користувача з системної таблиці <c>sqlite_master</c>,
    /// щоб продемонструвати зв'язок 1-до-1 (спільний PK/FK) між базовою та похідними таблицями.
    /// </summary>
    /// <param name="db">Активний контекст бази даних EF Core.</param>
    private static void PrintSqliteSchemas(DbContext db)
    {
        DbConnection conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            conn.Open();
        }

        using var cmd = conn.CreateCommand();
        // Виключаємо службові таблиці SQLite (sqlite_sequence тощо).
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
    /// Базовий клас ієрархії. Зберігається у таблиці <c>Users</c>.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Первинний ключ. У таблиці <c>Users</c> генерується через <c>AUTOINCREMENT</c>,
        /// а в таблицях <c>Employees</c> та <c>Managers</c> є одночасно PK і FK на <c>Users(Id)</c>.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача (спільна властивість для всієї ієрархії).
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Похідний клас працівника.
    /// Атрибут <see cref="TableAttribute"/> (<c>[Table("Employees")]</c>) або Fluent API
    /// <c>ToTable("Employees")</c> / <c>UseTptMappingStrategy()</c> вказують EF Core створити окрему таблицю
    /// зі спільним PK/FK <c>Id -> Users(Id)</c> та <c>ON DELETE CASCADE</c>.
    /// </summary>
    [Table("Employees")]
    public class Employee : User
    {
        /// <summary>
        /// Заробітна плата працівника. На відміну від TPH, у таблиці <c>Employees</c>
        /// цей стовпець має обмеження <c>INTEGER NOT NULL</c>, оскільки таблиця містить лише працівників.
        /// </summary>
        public int Salary { get; set; }
    }

    /// <summary>
    /// Похідний клас менеджера. Зберігається у таблиці <c>Managers</c> (PK/FK <c>Id -> Users(Id)</c>).
    /// </summary>
    [Table("Managers")]
    public class Manager : User
    {
        /// <summary>
        /// Назва відділу менеджера.
        /// </summary>
        public string? Departament { get; set; }
    }

    /// <summary>
    /// Контекст даних, що демонструє стратегію TPT (Table Per Type).
    /// </summary>
    public class TptContext : DbContext
    {
        /// <summary>
        /// Набір усіх користувачів (поліморфні запити з <c>LEFT JOIN</c>).
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Набір працівників (запити з <c>INNER JOIN</c> до таблиці <c>Users</c>).
        /// </summary>
        public DbSet<Employee> Employees { get; set; } = null!;

        /// <summary>
        /// Набір менеджерів (запити з <c>INNER JOIN</c> до таблиці <c>Users</c>).
        /// </summary>
        public DbSet<Manager> Managers { get; set; } = null!;

        /// <summary>
        /// Налаштовує підключення до локальної бази даних SQLite.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("lesson_4_2_tpt"));
        }

        /// <summary>
        /// Конфігурує стратегію відображення TPT для ієрархії <see cref="User"/>.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // У EF Core 7+ стратегію TPT можна задати одним викликом для базової сутності:
            // це автоматично створює окремі таблиці для всіх типів ієрархії без потреби дублювати ToTable().
            modelBuilder.Entity<User>().UseTptMappingStrategy();

            // Альтернативні способи (еквівалентні результати):
            // 1) Атрибут [Table("Employees")] / [Table("Managers")] на похідних класах
            // 2) modelBuilder.Entity<Employee>().ToTable("Employees");
            //    modelBuilder.Entity<Manager>().ToTable("Managers");
            //
            // Важливо про продуктивність та SQLite vs MS SQL Server:
            // - У великих ієрархіях TPT поліморфні запити (db.Users) породжують багато LEFT JOIN, що суттєво повільніше за TPH.
            // - У SQLite перевірка зовнішніх ключів (FK) працює лише при увімкненому PRAGMA foreign_keys = ON
            //   (провайдер Microsoft.Data.Sqlite у складі EF Core вмикає її за замовчуванням).
        }
    }
}

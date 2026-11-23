using System.Data.Common;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch05
{

/// <summary>
/// Урок 5.3: Підхід TPC (Table Per Concrete Type / Table Per Class — окрема таблиця для кожного конкретного класу).
/// Кожна таблиця містить усі стовпці, включно з успадкованими, що усуває JOIN при вибірці конкретного типу
/// та замінює JOIN на UNION ALL при поліморфних запитах.
/// </summary>
public class Lesson_5_03_TpcInheritance : ILesson
{
    public string Id => "5.3";
    public string Chapter => "05. Спадкування";
    public string Title => "Підхід TPC - Table Per Class";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/4.3.php";
    public int ChapterOrder => 5;
    public int LessonOrder => 3;

    /// <summary>
    /// Запускає демонстрацію стратегії спадкування TPC (Table Per Concrete Type / Table Per Class):
    /// виводить DDL-схему трьох незалежних таблиць у SQLite, додає сутності з унікальними GUID-ключами,
    /// показує згенерований SQL із <c>UNION ALL</c> та прямими <c>SELECT</c> без <c>JOIN</c>.
    /// </summary>
    public void Run()
    {
        using var db = new L4_3.TpcContext();
        // Перестворюємо базу даних з нуля (EnsureDeleted + EnsureCreated) для чистого старту.
        SqlServerDbHelper.RecreateSchema(db, "l5_03");

        // 1. Виводимо реальну схему 3 незалежних таблиць SQLite з sqlite_master
        SqlServerDbHelper.PrintSection("1. Схема таблиць у MS SQL Server (DDL)");
        Console.WriteLine(db.Database.GenerateCreateScript());

        // 2. Додаємо дані (унікальні GUID-ключі запобігають колізіям Id між таблицями)
        SqlServerDbHelper.PrintSection("2. Додавання даних у незалежні таблиці TPC");
        var user1 = new L4_3.User { Name = "Tom" };
        var user2 = new L4_3.User { Name = "Bob" };
        var employee = new L4_3.Employee { Name = "Sam", Salary = 500 };
        var manager = new L4_3.Manager { Name = "Robert", Departament = "IT" };

        // У TPC кожен об'єкт записується рівно в одну таблицю, що відповідає його конкретному типу:
        // user1, user2 -> Users; employee -> Employees; manager -> Managers.
        // На відміну від TPT, тут немає подвійних INSERT-ів, а на відміну від TPH — немає зайвих NULL-стовпців.
        db.Users.AddRange(user1, user2);
        db.Employees.Add(employee);
        db.Managers.Add(manager);
        db.SaveChanges();
        Console.WriteLine("Додано: 2 User у Users, 1 Employee у Employees, 1 Manager у Managers.");

        // 3. Демонструємо згенерований SQL через ToQueryString()
        SqlServerDbHelper.PrintSection("3. Згенерований SQL (ToQueryString)");
        Console.WriteLine("--- Запит db.Users (об'єднання таблиць через UNION ALL) ---");
        // При поліморфному запиті до базового типу EF Core об'єднує всі таблиці ієрархії через UNION ALL,
        // підставляючи NULL для відсутніх у конкретній таблиці стовпців і літерал ('User', 'Employee', 'Manager')
        // як віртуальний стовпець Discriminator для правильної матеріалізації CLR-типів.
        Console.WriteLine(db.Users.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Employees (прямий SELECT з однієї таблиці без JOIN) ---");
        // Найсильніша сторона TPC: вибірка конкретного типу виконується одним простим SELECT без JOIN і без WHERE Discriminator.
        Console.WriteLine(db.Employees.ToQueryString());
        Console.WriteLine();
        Console.WriteLine("--- Запит db.Managers (прямий SELECT з однієї таблиці без JOIN) ---");
        Console.WriteLine(db.Managers.ToQueryString());

        // 4. Вибірка даних
        SqlServerDbHelper.PrintSection("4. Результати вибірки");
        Console.WriteLine("Усі користувачі (db.Users):");
        // AsNoTracking() вимикає Change Tracker для швидкого читання без накладних витрат на відстеження.
        // EF Core все одно матеріалізує кожен рядок у точний CLR-тип (User, Employee, Manager).
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
    /// щоб показати три повністю незалежні таблиці без зовнішніх ключів між ними.
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
    /// Оскільки SQLite не підтримує <c>SEQUENCE</c> (послідовності БД), як у MS SQL Server чи PostgreSQL,
    /// використовуємо строковий GUID-ідентифікатор, що гарантує унікальність <see cref="Id"/>
    /// між різними таблицями ієрархії.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Первинний ключ у форматі строкового GUID.
        /// Якщо б використовувався <c>int</c> з <c>AUTOINCREMENT</c>, кожна таблиця (Users, Employees, Managers)
        /// почала б нумерацію з 1, що призвело б до колізій ключів при поліморфних запитах (<c>UNION ALL</c>).
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Ім'я користувача (дублюється як стовпець у кожній таблиці ієрархії).
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Похідний клас працівника. Таблиця <c>Employees</c> міститиме <c>Id</c>, <c>Name</c> та <c>Salary</c> (<c>NOT NULL</c>).
    /// </summary>
    public class Employee : User
    {
        /// <summary>
        /// Заробітна плата працівника.
        /// </summary>
        public int Salary { get; set; }
    }

    /// <summary>
    /// Похідний клас менеджера. Таблиця <c>Managers</c> міститиме <c>Id</c>, <c>Name</c> та <c>Departament</c>.
    /// </summary>
    public class Manager : User
    {
        /// <summary>
        /// Назва відділу менеджера.
        /// </summary>
        public string? Departament { get; set; }
    }

    /// <summary>
    /// Контекст даних, що демонструє стратегію TPC (Table Per Concrete Type).
    /// </summary>
    public class TpcContext : DbContext
    {
        /// <summary>
        /// Набір усіх користувачів (поліморфні запити через <c>UNION ALL</c>).
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Набір працівників (прямий запит до таблиці <c>Employees</c> без <c>JOIN</c>).
        /// </summary>
        public DbSet<Employee> Employees { get; set; } = null!;

        /// <summary>
        /// Набір менеджерів (прямий запит до таблиці <c>Managers</c> без <c>JOIN</c>).
        /// </summary>
        public DbSet<Manager> Managers { get; set; } = null!;

        /// <summary>
        /// Налаштовує підключення до локальної бази даних SQLite.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        /// <summary>
        /// Конфігурує стратегію відображення TPC для ієрархії <see cref="User"/>.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l5_03");
            // Вмикаємо стратегію TPC (Table Per Concrete Type), доступну з EF Core 7.0.
            // Примітка: якби базовий клас User був оголошений як abstract, EF Core не створював би таблицю Users взагалі —
            // існували б лише таблиці Employees та Managers.
            // Відмінність від MS SQL Server: у SQL Server для int-ключів у TPC EF Core автоматично створює спільний об'єкт SEQUENCE
            // (DEFAULT NEXT VALUE FOR ...), тоді як SQLite не має SEQUENCE, тому клієнтська генерація GUID є найнадійнішим підходом.
            modelBuilder.Entity<User>().UseTpcMappingStrategy();
        }
    }
}

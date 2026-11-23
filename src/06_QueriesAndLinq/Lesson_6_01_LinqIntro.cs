using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_1;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 6.1: Вступ до LINQ to Entities.
/// Демонструє два способи побудови запитів (синтаксис запитів LINQ та методи розширення)
/// і їх трансляцію в SQL-запити до SQLite.
/// </summary>
public class Lesson_6_01_LinqIntro : ILesson
{
    public string Id => "6.1";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Вступ до LINQ to Entities";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.1.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 1;

    /// <summary>
    /// Запускає демонстрацію двох синтаксисів LINQ to Entities (Query Syntax та Method Syntax)
    /// та порівнює згенерований ними SQL-код для SQLite.
    /// </summary>
    public void Run()
    {
        // 1. Створення та початкове заповнення бази даних
        // Кожен блок using створює окремий екземпляр DbContext із власним ChangeTracker,
        // щоб уникнути повернення закешованих у пам'яті об'єктів при наступних запитах.
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            Company microsoft = new() { Name = "Microsoft" };
            Company google = new() { Name = "Google" };
            db.Companies.AddRange(microsoft, google);

            // Присвоєння навігаційної властивості Company автоматично заповнить CompanyId після SaveChanges()
            User tom = new() { Name = "Tom", Age = 36, Company = microsoft };
            User bob = new() { Name = "Bob", Age = 39, Company = google };
            User alice = new() { Name = "Alice", Age = 28, Company = microsoft };
            User kate = new() { Name = "Kate", Age = 25, Company = google };
            db.Users.AddRange(tom, bob, alice, kate);

            // Генерує пакет INSERT-команд для SQLite; Company отримують згенеровані Id (INTEGER PRIMARY KEY),
            // які EF Core підставляє у зовнішній ключ CompanyId таблиці Users.
            db.SaveChanges();
        }

        // 2. Спосіб 1: Оператори LINQ (Query Syntax) — декларативний SQL-подібний синтаксис.
        // Компілятор C# перетворює цей синтаксис у ланцюжок методів розширення (Method Syntax) на етапі компіляції.
        SqliteDbHelper.PrintSection("1. Запит через оператори LINQ (Query Syntax)");
        using (var db = new AppDbContext())
        {
            // Include(u => u.Company) виконує Eager Loading (жадібне завантаження):
            // EF Core додасть INNER JOIN (або LEFT JOIN, якщо FK nullable) до таблиці Companies у SQL-запит.
            // У цей момент запит ще НЕ виконано — query має тип IQueryable<User> (відкладене виконання).
            var query = from user in db.Users.Include(u => u.Company)
                        where user.CompanyId == 1
                        select user;

            // ToQueryString() дає змогу побачити точний SQL, який EF Core відправить у SQLite,
            // без фактичного виконання запиту до бази даних.
            Console.WriteLine("Згенерований SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();

            // Матеріалізація запиту через ToList(): тут відбувається реальний запит SELECT до SQLite,
            // а повернуті сутності User та Company починають відстежуватися (Tracking) у ChangeTracker.
            var users = query.ToList();
            foreach (var user in users)
            {
                Console.WriteLine($"{user.Name} ({user.Age}) - {user.Company?.Name}");
            }
        }

        // 3. Спосіб 2: Методи розширення LINQ (Method Syntax / Fluent Syntax).
        // Зручніший для динамічного додавання умов, Include, AsNoTracking, Skip/Take тощо.
        SqliteDbHelper.PrintSection("2. Запит через методи розширення (Method Syntax)");
        using (var db = new AppDbContext())
        {
            // Where приймає Expression<Func<User, bool>> (дерево виразів), а не звичайний делегат Func<User, bool>.
            // Саме завдяки Expression EF Core аналізує AST виразу і транслює u.CompanyId == 1 у SQL-умову WHERE "u"."CompanyId" = 1.
            var query = db.Users
                .Include(u => u.Company)
                .Where(u => u.CompanyId == 1);

            // Обидва способи транслюються в ідентичний SQL-запит до SQLite.
            var users = query.ToList();
            foreach (var user in users)
            {
                Console.WriteLine($"{user.Name} ({user.Age}) - {user.Company?.Name}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_1
{
    /// <summary>Компанія (головна/батьківська сутність у зв'язку один-до-багатьох).</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії (у SQLite: INTEGER PRIMARY KEY AUTOINCREMENT).</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії (у SQLite: TEXT, допускає NULL).</summary>
        public string? Name { get; set; }

        /// <summary>Навігаційна властивість-колекція: усі користувачі цієї компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Користувач (залежна/дочірня сутність у зв'язку з Company).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача (у SQLite зберігається як INTEGER).</summary>
        public int Age { get; set; }

        /// <summary>Зовнішній ключ (Foreign Key) на таблицю Companies.</summary>
        public int CompanyId { get; set; }

        /// <summary>Навігаційна властивість до головної сутності Company.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.1, який керує підключенням до SQLite та мапінгом моделей.</summary>
    public class AppDbContext : DbContext
    {
        /// <summary>Набір сутностей Users (відображається на таблицю "Users").</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Набір сутностей Companies (відображається на таблицю "Companies").</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Налаштовує провайдер SQLite та рядок підключення до локального файлу БД.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_1"));
        }
    }
}

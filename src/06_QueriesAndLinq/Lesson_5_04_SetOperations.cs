using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_4;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.4: Операції з множинами: Union, Intersect, Except.
/// Демонструє об'єднання, перетин та різницю однорідних вибірок на рівні SQL (UNION, INTERSECT, EXCEPT).
/// </summary>
public class Lesson_5_04_SetOperations : ILesson
{
    public string Id => "5.4";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Операції з множинами: Union, Intersect, Except";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.4.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 5;

    /// <summary>
    /// Запускає демонстрацію операцій з множинами (Union, Intersect, Except) у EF Core
    /// та їх трансляцію в SQL-оператори SQLite.
    /// </summary>
    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            Company microsoft = new() { Name = "Microsoft" };
            Company google = new() { Name = "Google" };
            db.Companies.AddRange(microsoft, google);

            db.Users.AddRange(
                new User { Name = "Tom", Age = 36, Company = microsoft },
                new User { Name = "Bob", Age = 39, Company = google },
                new User { Name = "Alice", Age = 28, Company = microsoft },
                new User { Name = "Kate", Age = 25, Company = google }
            );
            db.SaveChanges();
        }

        // 1. Об'єднання (Union) — усуває дублікати
        SqliteDbHelper.PrintSection("1. Об'єднання вибірок (Union)");
        using (var db = new AppDbContext())
        {
            // Union транслюється в SQL UNION (який автоматично видаляє дублікати рядків, на відміну від Concat -> UNION ALL).
            // Користувачі віком < 30 АБО з ім'ям, що містить "Tom".
            var unionQuery = db.Users.Where(u => u.Age < 30)
                .Union(db.Users.Where(u => u.Name!.Contains("Tom")));

            Console.WriteLine("SQL для Union:");
            Console.WriteLine(unionQuery.ToQueryString());
            Console.WriteLine();

            foreach (var user in unionQuery.ToList())
                Console.WriteLine($"  {user.Name} ({user.Age})");

            // Об'єднання різнорідних таблиць можливе лише після приведення до однакового типу/структури проекції
            // (однакова кількість, типи та імена полів у Select).
            Console.WriteLine("\nУсі назви (користувачі + компанії) через однорідну проекцію Select:");
            var allNames = db.Users.Select(u => new { u.Name })
                .Union(db.Companies.Select(c => new { c.Name }))
                .ToList();

            foreach (var item in allNames)
                Console.WriteLine($"  {item.Name}");
        }

        // 2. Перетин (Intersect) — елементи, присутні в обох вибірках
        SqliteDbHelper.PrintSection("2. Перетин вибірок (Intersect)");
        using (var db = new AppDbContext())
        {
            // SQLite нативно підтримує оператор INTERSECT (як і MS SQL Server).
            // Якщо використовувати лише одну таблицю з простими умовами, часто ефективніше об'єднати предикати через && у Where,
            // але Intersect корисний для комбінування вже готових складних IQueryable-запитів.
            var intersectQuery = db.Users.Where(u => u.Age > 30)
                .Intersect(db.Users.Where(u => u.Name!.Contains("Tom")));

            foreach (var user in intersectQuery.ToList())
                Console.WriteLine($"  Age > 30 І ім'я містить 'Tom': {user.Name} ({user.Age})");
        }

        // 3. Різниця (Except) — елементи першої вибірки, яких немає у другій
        SqliteDbHelper.PrintSection("3. Різниця вибірок (Except)");
        using (var db = new AppDbContext())
        {
            // Транслюється в SQL EXCEPT (у SQLite та MS SQL Server; у деяких інших СУБД, наприклад старіших версіях MySQL,
            // міг емулюватися через NOT EXISTS).
            var selector1 = db.Users.Where(u => u.Age > 30);              // Tom (36), Bob (39)
            var selector2 = db.Users.Where(u => u.Name!.Contains("Tom")); // Tom (36)
            var exceptQuery = selector1.Except(selector2);

            foreach (var user in exceptQuery.ToList())
                Console.WriteLine($"  Age > 30 БЕЗ 'Tom': {user.Name} ({user.Age})");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_4
{
    /// <summary>Компанія (головна сутність).</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Користувачі, які працюють у компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Користувач (залежна сутність).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }

        /// <summary>Зовнішній ключ до таблиці Companies.</summary>
        public int CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.4 (операції з множинами).</summary>
    public class AppDbContext : DbContext
    {
        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Таблиця компаній.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Підключення до локальної БД SQLite для уроку 5.4.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_4"));
        }
    }
}

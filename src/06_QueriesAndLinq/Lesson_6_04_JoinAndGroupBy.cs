using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_3;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 6.4: З'єднання та групування таблиць (Join, GroupBy).
/// Демонструє явне з'єднання двох і трьох таблиць (Join / join ... equals) та групування (GroupBy / group ... by).
/// </summary>
public class Lesson_6_04_JoinAndGroupBy : ILesson
{
    public string Id => "6.4";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "З'єднання та групування таблиць (Join, GroupBy)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.3.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 4;

    /// <summary>
    /// Запускає демонстрацію явних з'єднань таблиць (Join для 2 і 3 таблиць)
    /// та групування (GroupBy) із переглядом згенерованого SQL для SQLite.
    /// </summary>
    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            Country usa = new() { Name = "USA" };
            Country ukraine = new() { Name = "Ukraine" };
            db.Countries.AddRange(usa, ukraine);

            Company microsoft = new() { Name = "Microsoft", Country = usa };
            Company google = new() { Name = "Google", Country = usa };
            Company softserve = new() { Name = "SoftServe", Country = ukraine };
            db.Companies.AddRange(microsoft, google, softserve);

            db.Users.AddRange(
                new User { Name = "Tom", Age = 36, Company = microsoft },
                new User { Name = "Bob", Age = 39, Company = google },
                new User { Name = "Alice", Age = 28, Company = microsoft },
                new User { Name = "Kate", Age = 25, Company = google },
                new User { Name = "Olena", Age = 30, Company = softserve }
            );
            db.SaveChanges();
        }

        // 1. З'єднання двох таблиць методом Join
        // На відміну від Include (який підтягує повні сутності з відстеженням у ChangeTracker),
        // Join зазвичай використовують для проекції лише потрібних колонок в анонімний тип або DTO (без tracking).
        SqliteDbHelper.PrintSection("1. З'єднання двох таблиць (Join)");
        using (var db = new AppDbContext())
        {
            // Join транслюється у SQL INNER JOIN "Companies" AS "c" ON "u"."CompanyId" = "c"."Id"
            var users = db.Users.Join(
                db.Companies,        // другий набір (таблиця для з'єднання)
                u => u.CompanyId,    // зовнішній ключ із першого набору (Users)
                c => c.Id,           // первинний ключ із другого набору (Companies)
                (u, c) => new        // результуюча проекція — SELECT вибере лише ці 3 колонки
                {
                    u.Name,
                    Company = c.Name,
                    u.Age
                });

            Console.WriteLine("Згенерований SQL:");
            Console.WriteLine(users.ToQueryString());
            Console.WriteLine();

            foreach (var u in users.ToList())
                Console.WriteLine($"{u.Name} ({u.Company}) - {u.Age}");
        }

        // 2. З'єднання трьох таблиць через оператори LINQ (join ... equals)
        // Для об'єднання 3+ таблиць Query Syntax часто читається простіше, ніж вкладені виклики .Join().Join().
        SqliteDbHelper.PrintSection("2. З'єднання трьох таблиць (Query Syntax)");
        using (var db = new AppDbContext())
        {
            // У SQLite це перетворюється на два послідовні INNER JOIN: Users -> Companies -> Countries.
            // Зауваження: замість явного join можна також використовувати навігаційні властивості (u.Company!.Country!.Name),
            // але явний join працює навіть тоді, коли навігаційні властивості в моделі відсутні.
            var users = from user in db.Users
                        join company in db.Companies on user.CompanyId equals company.Id
                        join country in db.Countries on company.CountryId equals country.Id
                        select new
                        {
                            user.Name,
                            Company = company.Name,
                            Country = country.Name,
                            user.Age
                        };

            foreach (var u in users.ToList())
                Console.WriteLine($"{u.Name} ({u.Company} - {u.Country}) - {u.Age}");
        }

        // 3. Групування даних (GroupBy та group ... by ... into)
        SqliteDbHelper.PrintSection("3. Групування (GroupBy)");
        using (var db = new AppDbContext())
        {
            // EF Core транслює GroupBy + Select з агрегатами (Count, Average, Sum, Min, Max)
            // безпосередньо в SQL-оператор GROUP BY на стороні SQLite.
            // Важливо: якщо після GroupBy НЕ викликати Select з агрегатною функцією, а спробувати
            // матеріалізувати самі групи (IGrouping<TKey, User>), EF Core 7+ може викинути помилку трансляції
            // або завантажити дані в пам'ять, оскільки SQL GROUP BY не повертає вкладені списки рядків.
            var groups = db.Users
                .GroupBy(u => u.Company!.Name)
                .Select(g => new
                {
                    CompanyName = g.Key,
                    Count = g.Count(),
                    AvgAge = g.Average(u => u.Age)
                });

            Console.WriteLine("Згенерований SQL:");
            Console.WriteLine(groups.ToQueryString());
            Console.WriteLine();

            foreach (var group in groups.ToList())
            {
                Console.WriteLine($"{group.CompanyName}: {group.Count} працівн., середній вік = {group.AvgAge:F1}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_3
{
    /// <summary>Країна (довідникова сутність верхнього рівня).</summary>
    public class Country
    {
        /// <summary>Первинний ключ країни.</summary>
        public int Id { get; set; }

        /// <summary>Назва країни.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Компанія (зв'язана з країною та користувачами).</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Зовнішній ключ до таблиці Countries.</summary>
        public int CountryId { get; set; }

        /// <summary>Навігаційна властивість до країни реєстрації компанії.</summary>
        public Country? Country { get; set; }

        /// <summary>Навігаційна колекція користувачів компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Користувач (залежна сутність, що належить певній компанії).</summary>
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

    /// <summary>Контекст бази даних для уроку 5.3 (Join та GroupBy).</summary>
    public class AppDbContext : DbContext
    {
        /// <summary>Таблиця країн.</summary>
        public DbSet<Country> Countries { get; set; } = null!;

        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Таблиця компаній.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Налаштовує підключення до локальної БД SQLite для уроку 5.3.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_3"));
        }
    }
}

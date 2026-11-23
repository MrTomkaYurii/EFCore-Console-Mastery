using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_9;

    /// <summary>
    /// Урок 4.6: Ліниве завантаження (Lazy Loading) через Microsoft.EntityFrameworkCore.Proxies.
    /// </summary>
    public class Lesson_4_06_LazyLoading : ILesson
    {
        public string Id => "4.6";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Ліниве завантаження (Lazy loading)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.9.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 6;

        /// <summary>
        /// Демонструє ліниве завантаження (Lazy Loading) за допомогою динамічних проксі-класів
        /// із пакета <c>Microsoft.EntityFrameworkCore.Proxies</c> та застереження щодо проблеми N+1.
        /// </summary>
        public void Run()
        {
            using (var db = new AppContextL3_9())
            {
                SqlServerDbHelper.RecreateSchema(db, "l4_06");

                var microsoft = new Company { Name = "Microsoft" };
                var google = new Company { Name = "Google" };
                db.Companies.AddRange(microsoft, google);

                db.Users.AddRange(
                    new User { Name = "Tom", Company = microsoft },
                    new User { Name = "Alice", Company = microsoft },
                    new User { Name = "Bob", Company = google },
                    new User { Name = "Kate", Company = google }
                );
                db.SaveChanges();
            }

            SqlServerDbHelper.PrintSection("1. Ліниве завантаження при зверненні до virtual-властивості Company");
            using (var db = new AppContextL3_9())
            {
                // Без Include! Перший SQL-запит витягує лише таблицю Users (SELECT Id, CompanyId, Name FROM Users).
                // Замість звичайних класів User EF Core створює в пам'яті динамічні проксі-спадкоємці (UserProxy).
                var users = db.Users.ToList();
                foreach (var user in users)
                {
                    // При першому зверненні до virtual-властивості user.Company проксі перехоплює getter
                    // і виконує додатковий SELECT * FROM Companies WHERE Id = @p0, якщо цієї компанії ще немає в Change Tracker.
                    // Увага: у циклі це призводить до проблеми N+1 запитів (1 запит за списком + до N запитів по елементах).
                    // Також Lazy Loading НЕ працює після закриття DbContext (ObjectDisposedException).
                    Console.WriteLine($"{user.Name} -> {user.Company?.Name}");
                }
            }

            SqlServerDbHelper.PrintSection("2. Ліниве завантаження колекції virtual Users у Company");
            using (var db = new AppContextL3_9())
            {
                var companies = db.Companies.ToList();
                foreach (var company in companies)
                {
                    Console.Write($"{company.Name}: ");
                    // При першому зверненні до virtual-колекції company.Users проксі виконує
                    // SELECT * FROM Users WHERE CompanyId = @p0 для кожної компанії окремо.
                    foreach (var user in company.Users)
                    {
                        Console.Write($"{user.Name} ");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_9
{
    // Вимоги UseLazyLoadingProxies():
    // 1) Класи сутностей мають бути public та НЕ sealed (бо Castle.DynamicProxy генерує їхніх спадкоємців у рантаймі).
    // 2) Усі навігаційні властивості, які мають ліниво завантажуватися, повинні бути public virtual.

    /// <summary>Компанія (клас не-sealed з <c>virtual</c>-колекцією для проксі-генерації).</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Віртуальна колекційна навігаційна властивість для лінивого завантаження користувачів.</summary>
        public virtual List<User> Users { get; set; } = new();
    }

    /// <summary>Користувач (клас не-sealed з <c>virtual</c>-посиланням на компанію).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Необов'язковий зовнішній ключ компанії.</summary>
        public int? CompanyId { get; set; }

        /// <summary>Віртуальна навігаційна властивість, яку перехоплює проксі для лінивого завантаження.</summary>
        public virtual Company? Company { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 3.9 (SQLite з увімкненим Lazy Loading).</summary>
    public class AppContextL3_9 : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("l4_06");
        /// <summary>Користувачі.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Компанії.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>
        /// Підключає <c>UseLazyLoadingProxies()</c> та провайдер SQLite.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // UseLazyLoadingProxies() вмикає генерацію проксі-об'єктів для всіх сутностей з virtual-навігаціями.
            // Альтернатива без проксі — ін'єкція ILazyLoader у конструктор сутності.
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlServer(SqlServerDbHelper.ConnectionString);
        }
    }
}

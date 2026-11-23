using System.Diagnostics;
using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch08.L8_1;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch08
{
    /// <summary>
    /// Урок 8.1: Скомпільовані запити (<c>EF.CompileQuery</c> та <c>EF.CompileAsyncQuery</c>).
    /// </summary>
    public class Lesson_8_01_CompiledQueries : ILesson
    {
        public string Id => "8.1";
        public string Chapter => "08. Додаткові теми";
        public string Title => "Скомпільовані запити (EF.CompileQuery / CompileAsyncQuery)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/8.1.php";
        public int ChapterOrder => 8;
        public int LessonOrder => 3;

        // Скомпільовані запити ОБОВ'ЯЗКОВО зберігають у static readonly полях!
        // Якщо викликати EF.CompileQuery щоразу в методі, компіляція дерева виразів виконуватиметься
        // повторно на кожен запит, що зробить код повільнішим за звичайний LINQ (і призведе до витоку пам'яті).

        // 1. Синхронний скомпільований запит: пошук одного користувача за Id з підвантаженням Company.
        // EF Core заздалегідь будує SQL (SELECT ... FROM Users LEFT JOIN Companies ... WHERE Id = @__id LIMIT 1)
        // і делегат матеріалізації, минаючи етап хешування Expression Tree при кожному виклику.
        private static readonly Func<CompiledQueriesContext, int, User?> UserByIdQuery =
            EF.CompileQuery((CompiledQueriesContext db, int id) =>
                db.Users
                    .Include(u => u.Company)
                    .FirstOrDefault(u => u.Id == id));

        // 2. Синхронний скомпільований запит, що повертає колекцію (IEnumerable<User>) з LIKE та фільтром за віком.
        // Важливо: запит повертає потоковий IEnumerable<User>, тому DbContext має залишатися відкритим
        // до завершення ітерації (наприклад, до виклику .ToList()).
        // EF.Functions.Like() транслюється в SQL-оператор LIKE (у SQLite для ASCII він за замовчуванням регістронезалежний).
        private static readonly Func<CompiledQueriesContext, string, int, IEnumerable<User>> UsersByNameAndAgeQuery =
            EF.CompileQuery((CompiledQueriesContext db, string namePattern, int minAge) =>
                db.Users
                    .Include(u => u.Company)
                    .Where(u => EF.Functions.Like(u.Name!, namePattern) && u.Age > minAge));

        // 3. Асинхронний скомпільований запит для одного об'єкта (повертає Task<User?>).
        // У лямбді всередині CompileAsyncQuery використовується синхронний FirstOrDefault,
        // але EF Core сам перетворює його на асинхронне виконання до БД (ExecuteReaderAsync).
        private static readonly Func<CompiledQueriesContext, int, Task<User?>> UserByIdAsyncQuery =
            EF.CompileAsyncQuery((CompiledQueriesContext db, int id) =>
                db.Users
                    .Include(u => u.Company)
                    .FirstOrDefault(u => u.Id == id));

        // 4. Асинхронний скомпільований запит для потоку результатів (повертає IAsyncEnumerable<User>).
        // Дозволяє обробляти рядки з SQLite по одному через await foreach без проміжного ToListAsync() у пам'яті.
        private static readonly Func<CompiledQueriesContext, string, IAsyncEnumerable<User>> UsersByCompanyAsyncQuery =
            EF.CompileAsyncQuery<CompiledQueriesContext, string, User>((CompiledQueriesContext db, string companyName) =>
                db.Users
                    .Include(u => u.Company)
                    .Where(u => u.Company != null && u.Company.Name == companyName)
                    .OrderBy(u => u.Name));

        /// <summary>
        /// Запускає демонстрацію скомпільованих запитів (<c>EF.CompileQuery</c> та <c>EF.CompileAsyncQuery</c>):
        /// 1) Синхронне виконання попередньо скомпільованих запитів.
        /// 2) Асинхронне виконання (<c>Task&lt;User?&gt;</c> та <c>IAsyncEnumerable&lt;User&gt;</c>).
        /// 3) Порівняння продуктивності зі звичайним LINQ-запитом на 200 ітераціях.
        /// </summary>
        public void Run()
        {
            var connStr = SqliteDbHelper.GetConnectionString("lesson_8_1_compiled_queries");

            // Ініціалізація чистої бази даних
            using (var setupDb = new CompiledQueriesContext(connStr))
            {
                SqliteDbHelper.RecreateDatabase(setupDb);

                var microsoft = new Company { Name = "Microsoft" };
                var google = new Company { Name = "Google" };
                setupDb.Companies.AddRange(microsoft, google);

                setupDb.Users.AddRange(
                    new User { Name = "Tom", Age = 36, Company = microsoft },
                    new User { Name = "Bob", Age = 39, Company = google },
                    new User { Name = "Alice", Age = 28, Company = microsoft },
                    new User { Name = "Kate", Age = 25, Company = google },
                    new User { Name = "Tomas", Age = 30, Company = microsoft },
                    new User { Name = "Tomek", Age = 42, Company = google }
                );
                setupDb.SaveChanges();
            }

            // -------------------------------------------------------------------------
            // 1. Виконання синхронних скомпільованих запитів (EF.CompileQuery)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("1. Синхронні скомпільовані запити (EF.CompileQuery)");

            using (var db = new CompiledQueriesContext(connStr))
            {
                var user = UserByIdQuery(db, 1);
                if (user is not null)
                {
                    Console.WriteLine($"UserByIdQuery(1): {user.Name} ({user.Age} р.), компанія: {user.Company?.Name}");
                }

                Console.WriteLine("\nUsersByNameAndAgeQuery('%Tom%', 30):");
                var matched = UsersByNameAndAgeQuery(db, "%Tom%", 30).ToList();
                foreach (var u in matched)
                {
                    Console.WriteLine($"  -> {u.Name} - {u.Age} р. ({u.Company?.Name})");
                }
            }

            // -------------------------------------------------------------------------
            // 2. Виконання асинхронних скомпільованих запитів (EF.CompileAsyncQuery)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("2. Асинхронні скомпільовані запити (EF.CompileAsyncQuery)");
            RunAsyncDemo(connStr).GetAwaiter().GetResult();

            // -------------------------------------------------------------------------
            // 3. Невеликий бенчмарк: 200 повторень звичайного vs скомпільованого запиту
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("3. Порівняння часу виконання (200 ітерацій у пам'яті / SQLite)");

            using (var db = new CompiledQueriesContext(connStr))
            {
                // Прогрів кешу запитів (перший виклик завжди виконує компіляцію/підготовку плану виконання)
                _ = db.Users.FirstOrDefault(u => u.Id == 1);
                _ = UserByIdQuery(db, 1);

                const int iterations = 200;

                // Звичайний LINQ-запит щоразу будує Expression Tree, обчислює його хеш і шукає у внутрішньому кеші EF Core
                var swRegular = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    _ = db.Users.Include(u => u.Company).FirstOrDefault(u => u.Id == 1);
                }
                swRegular.Stop();

                // Скомпільований запит пропускає побудову й хешування Expression Tree — одразу підставляє параметр @id у готовий делегат
                var swCompiled = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    _ = UserByIdQuery(db, 1);
                }
                swCompiled.Stop();

                Console.WriteLine($"Звичайний LINQ-запит ({iterations} разів):       {swRegular.ElapsedMilliseconds} мс");
                Console.WriteLine($"Скомпільований запит ({iterations} разів):       {swCompiled.ElapsedMilliseconds} мс");
            }
        }

        private static async Task RunAsyncDemo(string connStr)
        {
            await using var db = new CompiledQueriesContext(connStr);

            var user = await UserByIdAsyncQuery(db, 2);
            if (user is not null)
            {
                Console.WriteLine($"UserByIdAsyncQuery(2): {user.Name} ({user.Age} р.), компанія: {user.Company?.Name}");
            }

            Console.WriteLine("\nUsersByCompanyAsyncQuery('Microsoft'):");
            await foreach (var u in UsersByCompanyAsyncQuery(db, "Microsoft"))
            {
                Console.WriteLine($"  -> {u.Name} - {u.Age} р.");
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch08.L8_1
{
    /// <summary>
    /// Модель компанії (головна сутність у зв'язку 1:N з <see cref="User"/>).
    /// </summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Навігаційна властивість: список працівників компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>
    /// Модель користувача (залежна сутність із зовнішнім ключем <see cref="CompanyId"/>).
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }

        /// <summary>Зовнішній ключ до таблиці <c>Companies</c>.</summary>
        public int CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії користувача (підвантажується через <c>Include</c>).</summary>
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Контекст даних уроку 8.1 для демонстрації скомпільованих запитів.
    /// </summary>
    public class CompiledQueriesContext : DbContext
    {
        private readonly string _connectionString;

        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Таблиця компаній.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>
        /// Ініціалізує контекст із рядком підключення до SQLite.
        /// </summary>
        public CompiledQueriesContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }
    }
}

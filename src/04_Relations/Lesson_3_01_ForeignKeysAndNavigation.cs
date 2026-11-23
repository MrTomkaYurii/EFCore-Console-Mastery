using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_1;

    /// <summary>
    /// Урок 3.1: Зовнішні ключі та навігаційні властивості в EF Core.
    /// </summary>
    public class Lesson_3_01_ForeignKeysAndNavigation : ILesson
    {
        public string Id => "3.1";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Зовнішні ключі та навігаційні властивості";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.1.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 1;

        /// <summary>
        /// Демонструє два способи встановлення зв'язку один-до-багатьох: через навігаційну властивість та напряму через зовнішній ключ (FK).
        /// </summary>
        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Встановлення зв'язку через навігаційну властивість");
            using (var db = new AppContextL3_1())
            {
                // Перестворюємо БД: у SQLite зовнішні ключі (PRAGMA foreign_keys = ON) за замовчуванням вмикаються провайдером Microsoft.Data.Sqlite на рівні з'єднання.
                SqliteDbHelper.RecreateDatabase(db);

                var google = new Company { Name = "Google" };
                var microsoft = new Company { Name = "Microsoft" };

                // Достатньо встановити навігаційну властивість Company — Change Tracker EF Core побудує граф об'єктів
                // і після INSERT у таблицю Companies автоматично підставить згенерований Id у Users.CompanyId.
                var tom = new User { Name = "Tom", Company = google };
                var bob = new User { Name = "Bob", Company = microsoft };
                var alice = new User { Name = "Alice", Company = google };

                // AddRange додає кореневі та пов'язані сутності у стан Added; EF Core впорядковує INSERT (спочатку Companies, потім Users), щоб не порушити FK.
                db.Companies.AddRange(google, microsoft);
                db.Users.AddRange(tom, bob, alice);
                db.SaveChanges();

                // Include(u => u.Company) генерує INNER JOIN (оскільки CompanyId NOT NULL — зв'язок обов'язковий) і заповнює навігаційну властивість.
                foreach (var user in db.Users.Include(u => u.Company).ToList())
                {
                    Console.WriteLine($"{user.Name} (CompanyId={user.CompanyId}) -> {user.Company?.Name}");
                }
            }

            SqliteDbHelper.PrintSection("2. Встановлення зв'язку через зовнішній ключ (CompanyId)");
            using (var db = new AppContextL3_1())
            {
                // Завантажуємо лише існуючу компанію, щоб отримати її Id (або можемо передати відомий Id без запиту до БД).
                var microsoft = db.Companies.First(c => c.Name == "Microsoft");

                // Встановлюємо лише скалярну властивість CompanyId — це зручно у веб-API, коли з клієнта приходить тільки ID компанії без об'єкта Company.
                var kate = new User { Name = "Kate", CompanyId = microsoft.Id };
                db.Users.Add(kate);
                // SQLite перевірить обмеження FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id"): якщо такого Id немає, виникне SqliteException (Error 19).
                db.SaveChanges();
            }

            // Перевіряємо в новому контексті (із чистим Change Tracker), що навігаційна властивість підтягується з БД коректно.
            using (var db = new AppContextL3_1())
            {
                var kate = db.Users.Include(u => u.Company).First(u => u.Name == "Kate");
                Console.WriteLine($"{kate.Name} (CompanyId={kate.CompanyId}) -> {kate.Company?.Name}");

                Console.WriteLine("\nКомпанії та їхні співробітники:");
                // Тут Include(c => c.Users) генерує LEFT JOIN у SQLite, щоб повернути навіть ті компанії, у яких 0 користувачів.
                foreach (var comp in db.Companies.Include(c => c.Users).ToList())
                {
                    Console.WriteLine($"- {comp.Name}: {string.Join(", ", comp.Users.Select(u => u.Name))}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_1
{
    /// <summary>
    /// Головна (principal) сутність — компанія. На неї посилається залежна таблиця Users.
    /// </summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії (INTEGER PRIMARY KEY AUTOINCREMENT у SQLite).</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії (TEXT NULL у SQLite).</summary>
        public string? Name { get; set; }

        /// <summary>
        /// Колекційна навігаційна властивість (inverse navigation): усі користувачі компанії.
        /// Ініціалізація через new() запобігає NullReferenceException при додаванні елементів у пам'яті.
        /// </summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>
    /// Залежна (dependent) сутність — користувач, що містить зовнішній ключ на <see cref="Company"/>.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>
        /// Зовнішній ключ (FK) за конвенцією EF Core: &lt;НазваНавігаційноїВластивості&gt;Id.
        /// Оскільки тип не-nullable (<c>int</c>), EF Core створює стовпець INTEGER NOT NULL, а зв'язок вважається обов'язковим (required).
        /// </summary>
        public int CompanyId { get; set; }

        /// <summary>Посилальна навігаційна властивість (reference navigation) до головної сутності Company.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Контекст бази даних для уроку 3.1 (SQLite).
    /// </summary>
    public class AppContextL3_1 : DbContext
    {
        /// <summary>Набір сутностей користувачів (таблиця Users).</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Набір сутностей компаній (таблиця Companies).</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Налаштовує підключення до локальної БД SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_1"));
        }
    }
}

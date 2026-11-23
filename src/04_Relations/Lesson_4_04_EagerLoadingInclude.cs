using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_3;

    /// <summary>
    /// Урок 4.4: Жадне завантаження пов'язаних даних (Eager Loading) через Include та ThenInclude.
    /// </summary>
    public class Lesson_4_04_EagerLoadingInclude : ILesson
    {
        public string Id => "4.4";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Завантаження пов'язаних даних. Метод Include";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.3.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 4;

        /// <summary>
        /// Демонструє жадне завантаження (Eager Loading) через методи <c>Include</c> та <c>ThenInclude</c>
        /// для отримання багаторівневих зв'язків одним SQL-запитом.
        /// </summary>
        public void Run()
        {
            using (var db = new AppContextL3_3())
            {
                SqlServerDbHelper.RecreateSchema(db, "l4_04");

                var ukraine = new Country { Name = "Ukraine" };
                var usa = new Country { Name = "USA" };
                db.Countries.AddRange(ukraine, usa);

                var kyivTech = new Company { Name = "KyivTech", Country = ukraine };
                var microsoft = new Company { Name = "Microsoft", Country = usa };
                db.Companies.AddRange(kyivTech, microsoft);

                var dev = new Position { Name = "Developer" };
                var manager = new Position { Name = "Manager" };
                db.Positions.AddRange(dev, manager);

                db.Users.AddRange(
                    new User { Name = "Taras", Company = kyivTech, Position = dev },
                    new User { Name = "Olena", Company = kyivTech, Position = manager },
                    new User { Name = "Tom", Company = microsoft, Position = dev }
                );
                db.SaveChanges();
            }

            SqlServerDbHelper.PrintSection("1. Без Include навігаційні властивості дорівнюють null");
            using (var db = new AppContextL3_3())
            {
                // За замовчуванням EF Core НЕ завантажує пов'язані сутності (виконується простий SELECT * FROM Users).
                // Якщо контекст чистий (без раніше завантажених Company), u.Company буде null.
                var usersWithoutInclude = db.Users.ToList();
                foreach (var u in usersWithoutInclude)
                {
                    Console.WriteLine($"{u.Name} -> Company: {u.Company?.Name ?? "NULL (не завантажено)"}");
                }
            }

            SqlServerDbHelper.PrintSection("2. Жадне завантаження через Include + ThenInclude");
            using (var db = new AppContextL3_3())
            {
                // Include(u => u.Company) та Include(u => u.Position) підвантажують сутності першого рівня (через LEFT JOIN, бо FK nullable).
                // ThenInclude(c => c!.Country) спускається на рівень нижче від Company до Country (INNER JOIN, бо CountryId NOT NULL).
                // Увесь граф повертається одним SQL-запитом у SQLite.
                var users = db.Users
                    .Include(u => u.Company)
                        .ThenInclude(c => c!.Country)
                    .Include(u => u.Position)
                    .ToList();

                foreach (var u in users)
                {
                    Console.WriteLine($"{u.Name} | Посада: {u.Position?.Name} | Компанія: {u.Company?.Name} ({u.Company?.Country?.Name})");
                }
            }

            SqlServerDbHelper.PrintSection("3. Зворотне завантаження колекцій (Company -> Users)");
            using (var db = new AppContextL3_3())
            {
                // Include на колекційній властивості (Company.Users) генерує LEFT JOIN між Companies та Users.
                // Зауваження: при кількох паралельних колекційних Include може виникати декартовий добуток (Cartesian explosion),
                // для уникнення якого в EF Core використовують .AsSplitQuery().
                var companies = db.Companies
                    .Include(c => c.Users)
                    .ToList();

                foreach (var c in companies)
                {
                    Console.WriteLine($"Компанія {c.Name}: {string.Join(", ", c.Users.Select(u => u.Name))}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_3
{
    /// <summary>Країна розташування компанії.</summary>
    public class Country
    {
        /// <summary>Первинний ключ країни.</summary>
        public int Id { get; set; }

        /// <summary>Назва країни.</summary>
        public string? Name { get; set; }

        /// <summary>Колекція компаній у цій країні.</summary>
        public List<Company> Companies { get; set; } = new();
    }

    /// <summary>Компанія, що належить певній країні та має користувачів.</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }

        /// <summary>Обов'язковий зовнішній ключ на країну (NOT NULL).</summary>
        public int CountryId { get; set; }

        /// <summary>Навігаційна властивість до країни.</summary>
        public Country? Country { get; set; }

        /// <summary>Колекція користувачів компанії.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Посада співробітника.</summary>
    public class Position
    {
        /// <summary>Первинний ключ посади.</summary>
        public int Id { get; set; }

        /// <summary>Назва посади.</summary>
        public string? Name { get; set; }

        /// <summary>Користувачі на цій посаді.</summary>
        public List<User> Users { get; set; } = new();
    }

    /// <summary>Співробітник із посиланнями на компанію та посаду.</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Необов'язковий зовнішній ключ компанії (NULL у SQLite).</summary>
        public int? CompanyId { get; set; }

        /// <summary>Навігаційна властивість до компанії.</summary>
        public Company? Company { get; set; }

        /// <summary>Необов'язковий зовнішній ключ посади (NULL у SQLite).</summary>
        public int? PositionId { get; set; }

        /// <summary>Навігаційна властивість до посади.</summary>
        public Position? Position { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 3.3 (SQLite).</summary>
    public class AppContextL3_3 : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("l4_04");
        /// <summary>Користувачі.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Компанії.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Країни.</summary>
        public DbSet<Country> Countries { get; set; } = null!;

        /// <summary>Посади.</summary>
        public DbSet<Position> Positions { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }
    }
}

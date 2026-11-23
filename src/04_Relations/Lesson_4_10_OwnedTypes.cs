using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_7;

    /// <summary>
    /// Урок 4.10: Власні типи (Owned Entity Types) через [Owned] та Fluent API OwnsOne.
    /// </summary>
    public class Lesson_4_10_OwnedTypes : ILesson
    {
        public string Id => "4.10";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Власні типи (Owned Types)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.7.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 10;

        /// <summary>
        /// Демонструє роботу з власними типами (Owned Entity Types) через атрибут <c>[Owned]</c> та Fluent API <c>OwnsOne</c>,
        /// які за замовчуванням зберігаються в тій самій таблиці, що й сутність-власник (table splitting).
        /// </summary>
        public void Run()
        {
            SqlServerDbHelper.PrintSection("1. Збереження Owned Types в одній таблиці з власником");
            using (var db = new AppContextL3_7())
            {
                SqlServerDbHelper.RecreateSchema(db, "l4_10");

                // У SQLite створюється лише одна таблиця "Users" зі стовпцями:
                // Id, Login, Password, Profile_Name, Profile_Age, Address_City, Address_Street.
                var user1 = new User
                {
                    Login = "login1",
                    Password = "pass1234",
                    Profile = new UserProfile { Name = "Tom", Age = 23 },
                    Address = new Address { City = "Kyiv", Street = "Khreshchatyk, 1" }
                };

                var user2 = new User
                {
                    Login = "login2",
                    Password = "5678word2",
                    Profile = new UserProfile { Name = "Alice", Age = 27 },
                    Address = new Address { City = "Lviv", Street = "Rynok Sq, 10" }
                };

                db.Users.AddRange(user1, user2);
                db.SaveChanges();
            }

            SqlServerDbHelper.PrintSection("2. Читання без Include (дані лежать у тій самій таблиці Users)");
            using (var db = new AppContextL3_7())
            {
                // На відміну від звичайних навігаційних властивостей, Owned-типи завантажуються АВТОМАТИЧНО без Include(),
                // оскільки фізично вони є стовпцями тієї самої таблиці Users (SELECT * FROM Users без JOIN).
                // Важливо: якщо всі стовпці Owned-типу в рядку БД дорівнюють NULL, EF Core залишить саму властивість (напр. u.Address) рівною null.
                var users = db.Users.ToList();
                foreach (var u in users)
                {
                    Console.WriteLine(
                        $"Login: {u.Login} | Name: {u.Profile?.Name}, Age: {u.Profile?.Age} | " +
                        $"Address: {u.Address?.City}, {u.Address?.Street}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_7
{
    /// <summary>Сутність-власник (Owner), до таблиці якої вбудовуються Owned-типи.</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Логін користувача.</summary>
        public string? Login { get; set; }

        /// <summary>Пароль користувача.</summary>
        public string? Password { get; set; }

        /// <summary>Власний тип, налаштований через атрибут <c>[Owned]</c>.</summary>
        public UserProfile? Profile { get; set; }

        /// <summary>Власний тип, налаштований через Fluent API <c>OwnsOne()</c>.</summary>
        public Address? Address { get; set; }
    }

    /// <summary>
    /// Власний тип (Owned Type) не має власного первинного ключа <c>Id</c> у C#-моделі —
    /// він логічно належить лише одному власнику і не може існувати окремо як <c>DbSet&lt;UserProfile&gt;</c>.
    /// </summary>
    [Owned]
    public class UserProfile
    {
        /// <summary>Ім'я користувача (стовпець Profile_Name у таблиці Users).</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача (стовпець Profile_Age у таблиці Users).</summary>
        public int Age { get; set; }
    }

    /// <summary>Ще один власний тип, налаштований у <c>OnModelCreating</c> через <c>OwnsOne</c>.</summary>
    public class Address
    {
        /// <summary>Місто (стовпець Address_City у таблиці Users).</summary>
        public string? City { get; set; }

        /// <summary>Вулиця (стовпець Address_Street у таблиці Users).</summary>
        public string? Street { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 3.7 (SQLite).</summary>
    public class AppContextL3_7 : DbContext
    {
        /// <summary>Користувачі (містить також вбудовані стовпці Profile_* та Address_*).</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        /// <summary>
        /// Налаштовує власний тип <see cref="Address"/> через Fluent API.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l4_10");
            // OwnsOne вбудовує властивості Address у таблицю Users зі стовпцями Address_City, Address_Street.
            // При потребі можна викликати .ToTable("UserAddresses"), щоб винести Owned-тип в окрему таблицю зі спільним PK,
            // або .OwnsMany(...) для колекції власних об'єктів (які завжди виносяться в окрему таблицю або JSON).
            modelBuilder.Entity<User>().OwnsOne(u => u.Address);
        }
    }
}

using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_7;

    /// <summary>
    /// Урок 3.7: Власні типи (Owned Entity Types) через [Owned] та Fluent API OwnsOne.
    /// </summary>
    public class Lesson_3_7_OwnedTypes : ILesson
    {
        public string Id => "3.7";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Власні типи (Owned Types)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.7.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 10;

        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Збереження Owned Types в одній таблиці з власником");
            using (var db = new AppContextL3_7())
            {
                SqliteDbHelper.RecreateDatabase(db);

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

            SqliteDbHelper.PrintSection("2. Читання без Include (дані лежать у тій самій таблиці Users)");
            using (var db = new AppContextL3_7())
            {
                // Owned-типи завантажуються автоматично разом із сутністю-власником
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
    /// <summary>Сутність-власник (Owner).</summary>
    public class User
    {
        public int Id { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }

        /// <summary>Власний тип, налаштований через атрибут [Owned].</summary>
        public UserProfile? Profile { get; set; }

        /// <summary>Власний тип, налаштований через Fluent API OwnsOne().</summary>
        public Address? Address { get; set; }
    }

    /// <summary>Власний тип не має власного первинного ключа Id.</summary>
    [Owned]
    public class UserProfile
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    /// <summary>Ще один власний тип, налаштований у OnModelCreating через OwnsOne.</summary>
    public class Address
    {
        public string? City { get; set; }
        public string? Street { get; set; }
    }

    public class AppContextL3_7 : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_7"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Усі властивості Address потраплять у таблицю Users зі стовпцями Address_City, Address_Street
            modelBuilder.Entity<User>().OwnsOne(u => u.Address);
        }
    }
}

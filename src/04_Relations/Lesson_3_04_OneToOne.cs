using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_4;

    /// <summary>
    /// Урок 3.4: Відношення один до одного (One-to-One).
    /// </summary>
    public class Lesson_3_04_OneToOne : ILesson
    {
        public string Id => "3.4";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Відношення один до одного";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.4.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 7;

        /// <summary>
        /// Демонструє створення, читання, оновлення та каскадне видалення у відношенні один-до-одного (1:1).
        /// </summary>
        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Створення та читання зв'язку 1:1 (User <-> UserProfile)");
            using (var db = new AppContextL3_4())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var user1 = new User { Login = "login1", Password = "pass1234" };
                var user2 = new User { Login = "login2", Password = "5678word2" };
                db.Users.AddRange(user1, user2);

                // UserProfile — залежна сутність: містить зовнішній ключ UserId.
                // Щоб гарантувати зв'язок саме 1:1 (а не 1:N), EF Core автоматично створює у SQLite
                // CREATE UNIQUE INDEX "IX_UserProfiles_UserId" ON "UserProfiles" ("UserId").
                var profile1 = new UserProfile { Name = "Tom", Age = 22, User = user1 };
                var profile2 = new UserProfile { Name = "Alice", Age = 27, User = user2 };
                db.UserProfiles.AddRange(profile1, profile2);

                db.SaveChanges();
            }

            using (var db = new AppContextL3_4())
            {
                // Include(u => u.Profile) виконує LEFT JOIN по UserProfiles.UserId = Users.Id.
                var users = db.Users.Include(u => u.Profile).ToList();
                foreach (var u in users)
                {
                    Console.WriteLine($"Login: {u.Login} | Name: {u.Profile?.Name}, Age: {u.Profile?.Age}");
                }
            }

            SqliteDbHelper.PrintSection("2. Оновлення та видалення у зв'язку 1:1");
            using (var db = new AppContextL3_4())
            {
                var alice = db.Users.Include(u => u.Profile).First(u => u.Login == "login2");
                if (alice.Profile != null)
                {
                    // Change Tracker фіксує зміну Age у пов'язаному об'єкті Profile (стан Modified) і оновлює лише таблицю UserProfiles.
                    alice.Profile.Age = 28;
                }

                // Оскільки UserId у UserProfile не-nullable (int), діє ON DELETE CASCADE:
                // видалення головного об'єкта User автоматично видаляє і пов'язаний запис у UserProfiles.
                var tom = db.Users.First(u => u.Login == "login1");
                db.Users.Remove(tom);
                db.SaveChanges();
            }

            using (var db = new AppContextL3_4())
            {
                Console.WriteLine($"Залишилось користувачів: {db.Users.Count()}, профілів: {db.UserProfiles.Count()}");
                var remaining = db.UserProfiles.Include(p => p.User).First();
                Console.WriteLine($"Профіль: {remaining.Name}, Age={remaining.Age}, Login={remaining.User?.Login}");
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_4
{
    /// <summary>Головна (principal) сутність облікового запису користувача.</summary>
    public class User
    {
        /// <summary>Первинний ключ облікового запису.</summary>
        public int Id { get; set; }

        /// <summary>Логін користувача.</summary>
        public string? Login { get; set; }

        /// <summary>Пароль користувача.</summary>
        public string? Password { get; set; }

        /// <summary>Посилальна навігаційна властивість до профілю (зворотний бік зв'язку 1:1).</summary>
        public UserProfile? Profile { get; set; }
    }

    /// <summary>
    /// Залежна (dependent) сутність профілю: містить зовнішній ключ <see cref="UserId"/>,
    /// на який EF Core накладає унікальний індекс (<c>UNIQUE</c>) для забезпечення кратності 1:1.
    /// </summary>
    public class UserProfile
    {
        /// <summary>Власний первинний ключ профілю (альтернативно PK міг би бути одночасно і FK — shared primary key).</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }

        /// <summary>Зовнішній ключ на таблицю Users (унікальний у межах таблиці UserProfiles).</summary>
        public int UserId { get; set; }

        /// <summary>Навігаційна властивість до головного об'єкта <see cref="User"/>.</summary>
        public User? User { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 3.4 (SQLite).</summary>
    public class AppContextL3_4 : DbContext
    {
        /// <summary>Облікові записи.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Профілі користувачів.</summary>
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_4"));
        }
    }
}

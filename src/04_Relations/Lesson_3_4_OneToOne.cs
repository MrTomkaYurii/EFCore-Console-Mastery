using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_4;

    /// <summary>
    /// Урок 3.4: Відношення один до одного (One-to-One).
    /// </summary>
    public class Lesson_3_4_OneToOne : ILesson
    {
        public string Id => "3.4";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Відношення один до одного";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.4.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 7;

        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Створення та читання зв'язку 1:1 (User <-> UserProfile)");
            using (var db = new AppContextL3_4())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var user1 = new User { Login = "login1", Password = "pass1234" };
                var user2 = new User { Login = "login2", Password = "5678word2" };
                db.Users.AddRange(user1, user2);

                // UserProfile містить зовнішній ключ UserId, на який EF Core створює UNIQUE INDEX
                var profile1 = new UserProfile { Name = "Tom", Age = 22, User = user1 };
                var profile2 = new UserProfile { Name = "Alice", Age = 27, User = user2 };
                db.UserProfiles.AddRange(profile1, profile2);

                db.SaveChanges();
            }

            using (var db = new AppContextL3_4())
            {
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
                    alice.Profile.Age = 28;
                }

                // Видалення головного об'єкта User каскадно видалить і його UserProfile
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
    /// <summary>Головна сутність облікового запису.</summary>
    public class User
    {
        public int Id { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
        public UserProfile? Profile { get; set; }
    }

    /// <summary>Залежна сутність профілю (містить FK UserId з унікальним індексом).</summary>
    public class UserProfile
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
    }

    public class AppContextL3_4 : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_4"));
        }
    }
}

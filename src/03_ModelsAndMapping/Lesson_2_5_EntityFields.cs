using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.5: Використання полів сутності.
/// Демонструє інкапсуляцію даних через приватні поля (backing fields)
/// та їх налаштування в Fluent API за допомогою Property("...") і HasField("...").
/// </summary>
public class Lesson_2_5_EntityFields : ILesson
{
    public string Id => "2.5";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Використання полів сутності";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.5.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 5;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.5. Використання полів сутності");

        using (var db = new L2_5.ApplicationContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            var bob = new L2_5.User("Bob", 30);
            var kate = new L2_5.User("Kate", 29);

            db.Users.AddRange(bob, kate);
            db.SaveChanges();
        }

        // Читаємо у новому контексті, щоб переконатися, що EF Core заповнює приватні поля
        Console.WriteLine("Користувачі, прочитані з БД у приватні поля:");
        using (var db = new L2_5.ApplicationContext())
        {
            var users = db.Users.ToList();
            foreach (var user in users)
            {
                user.Print();
            }

            // Пошук за полем без публічної властивості (через EF.Property)
            var kateFromDb = db.Users
                .FirstOrDefault(u => EF.Property<string>(u, "name") == "Kate");
            Console.WriteLine($"\nЗнайдено через EF.Property<string>(u, \"name\"): Id={kateFromDb?.Id}, Age={kateFromDb?.Age}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_5
{
    /// <summary>
    /// Сутність із повною інкапсуляцією: усі дані зберігаються в приватних полях.
    /// Id та Age доступні лише для читання, а name взагалі приховане з публічного інтерфейсу.
    /// </summary>
    public class User
    {
        // Ініціалізуємо поле, щоб уникнути попередження компілятора CS0649 (EF Core заповнить його через reflection)
        private int id = 0;
        private string name;
        private int age;

        public int Id => id;
        public int Age => age;

        public User(string name, int age)
        {
            this.name = name;
            this.age = age;
        }

        public void Print() => Console.WriteLine($"  {id}. {name} - {age}");
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_5"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Прив'язуємо властивості лише для читання до їхніх приватних полів
            modelBuilder.Entity<User>().Property("Id").HasField("id");
            modelBuilder.Entity<User>().Property("Age").HasField("age");

            // Поле без публічної властивості: реєструємо безпосередньо як властивість моделі
            modelBuilder.Entity<User>().Property("name");
        }
    }
}

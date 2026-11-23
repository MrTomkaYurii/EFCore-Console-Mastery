using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.5: Використання полів сутності.
/// Демонструє інкапсуляцію даних через приватні поля (backing fields)
/// та їх налаштування в Fluent API за допомогою Property("...") і HasField("...").
/// </summary>
public class Lesson_2_05_EntityFields : ILesson
{
    public string Id => "2.5";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Використання полів сутності";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.5.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 5;

    /// <summary>
    /// Запускає демонстрацію інкапсуляції сутності через приватні поля:
    /// створює об'єкти через конструктор, зберігає в SQLite, а потім читає у новому контексті
    /// та виконує LINQ-пошук по прихованому полю через EF.Property.
    /// </summary>
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

        // Читаємо у новому контексті, щоб переконатися, що EF Core матеріалізує сутності
        // напряму через приватні поля (без публічних set-аксесорів і без конструктора за замовчуванням?
        // Ні: тут використовується конструктор User(string name, int age), параметри якого збігаються з іменами властивостей/полів).
        Console.WriteLine("Користувачі, прочитані з БД у приватні поля:");
        using (var db = new L2_5.ApplicationContext())
        {
            var users = db.Users.ToList();
            foreach (var user in users)
            {
                user.Print();
            }

            // Пошук за полем без публічної властивості (через EF.Property<T>):
            // EF Core транслює цей виклик у SQL: WHERE "name" = 'Kate' LIMIT 1.
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

        /// <summary>
        /// Ідентифікатор користувача лише для читання (backing field: <c>id</c>).
        /// </summary>
        public int Id => id;

        /// <summary>
        /// Вік користувача лише для читання (backing field: <c>age</c>).
        /// </summary>
        public int Age => age;

        /// <summary>
        /// Конструктор із параметрами, які EF Core також може використовувати при матеріалізації
        /// (імена параметрів name, age збігаються з властивостями/полями моделі без урахування регістру).
        /// </summary>
        public User(string name, int age)
        {
            this.name = name;
            this.age = age;
        }

        /// <summary>Виводить інкапсульовані дані користувача в консоль.</summary>
        public void Print() => Console.WriteLine($"  {id}. {name} - {age}");
    }

    /// <summary>
    /// Контекст бази даних для уроку 2.5, що налаштовує доступ EF Core до приватних полів сутності User.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        /// <summary>Набір користувачів у таблиці "Users".</summary>
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_5"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Прив'язуємо властивості лише для читання (без set-аксесора) до їхніх приватних полів.
            // Без HasField(...) EF Core проігнорував би Id та Age, бо вони не мають setter'а.
            // У SQLite для Id все одно буде створено INTEGER PRIMARY KEY AUTOINCREMENT.
            modelBuilder.Entity<User>().Property("Id").HasField("id");
            modelBuilder.Entity<User>().Property("Age").HasField("age");

            // Поле без публічної властивості: реєструємо безпосередньо за іменем поля.
            // У БД з'явиться стовпець "name" (TEXT NOT NULL), до якого можна звертатися в LINQ через EF.Property.
            modelBuilder.Entity<User>().Property("name");
        }
    }
}

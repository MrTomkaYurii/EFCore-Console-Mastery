using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.10: Конструктори сутностей.
/// Показує, як EF Core використовує параметризовані конструктори (у тому числі private)
/// під час матеріалізації об'єктів із бази даних, а також обмеження щодо навігаційних властивостей.
/// </summary>
public class Lesson_2_10_EntityConstructors : ILesson
{
    public string Id => "2.10";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Конструктори сутностей";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.10.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 4;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.10. Конструктори сутностей");

        // Крок 1: Створення та збереження даних у першому контексті
        using (var db = new L2_10.ApplicationContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            var company = new L2_10.Company("JetBrains");
            var tom = new L2_10.User("Tom", 37) { Company = company };
            var bob = new L2_10.User("Bob", 41) { Company = company };

            db.Users.AddRange(tom, bob);
            db.SaveChanges();
        }

        // Крок 2: Читання з БД у новому контексті — EF Core викликає конструктори при матеріалізації
        Console.WriteLine("\nОтримання даних з БД (EF Core викликає конструктори сутностей):");
        using (var db = new L2_10.ApplicationContext())
        {
            var users = db.Users.Include(u => u.Company).ToList();
            foreach (var user in users)
            {
                Console.WriteLine($"  Id={user.Id}, Name={user.Name}, Age={user.Age}, Company={user.Company?.Name}");
            }
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_10
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        // Навігаційна властивість НЕ може передаватися через конструктор EF Core
        public Company? Company { get; set; }

        // Параметризований конструктор: імена параметрів збігаються з властивостями (без урахування регістру).
        // Id у конструкторі немає — EF Core встановить його напряму через властивість.
        public User(string name, int age)
        {
            Name = name;
            Age = age;
            Console.WriteLine($"  [Конструктор User(string, int)] Створено об'єкт {name}, {age}");
        }
    }

    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();

        // EF Core може викликати навіть приватні або захищені конструктори
        private Company()
        {
            Name = string.Empty;
        }

        public Company(string name)
        {
            Name = name;
            Console.WriteLine($"  [Конструктор Company(string)] Створено компанію {name}");
        }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Company> Companies => Set<Company>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_10"));
        }
    }
}

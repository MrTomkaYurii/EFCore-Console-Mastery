using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 1.2. Перший застосунок на EF Core.
/// Класичний приклад Metanit: модель User, ApplicationContext, додавання та читання записів.
/// </summary>
public class Lesson_1_2_FirstApp : ILesson
{
    public string Id => "1.2";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Перший застосунок на EF Core";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.2.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 2;

    public void Run()
    {
        var connectionString = SqliteDbHelper.GetConnectionString("lesson_1_2");

        // Створюємо контекст даних та гарантуємо чисту БД для демонстрації
        using (var db = new L1_2.ApplicationContext(connectionString))
        {
            SqliteDbHelper.RecreateDatabase(db);

            SqliteDbHelper.PrintSection("1. Створення та збереження об'єктів User");
            var tom = new L1_2.User { Name = "Tom", Age = 33 };
            var alice = new L1_2.User { Name = "Alice", Age = 26 };

            // Додаємо об'єкти до відстеження контекстом (стан Added)
            db.Users.Add(tom);
            db.Users.Add(alice);

            // Генеруємо та виконуємо SQL INSERT у базі даних
            int affected = db.SaveChanges();
            Console.WriteLine($"Об'єкти успішно збережено (змінено рядків у БД: {affected}).");

            SqliteDbHelper.PrintSection("2. Отримання об'єктів із БД");
            var users = db.Users.ToList();
            Console.WriteLine("Список користувачів:");
            foreach (var u in users)
            {
                Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch01.L1_2
{
    /// <summary>
    /// Модель користувача. Властивість Id за конвенцією стає первинним ключем з автоінкрементом.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст даних застосунку.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        private readonly string _connectionString;

        /// <summary>
        /// Ініціалізація через Set&lt;User&gt;() дозволяє уникнути попереджень nullable-аналізатора (CS8618).
        /// </summary>
        public DbSet<User> Users => Set<User>();

        public ApplicationContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }
    }
}

using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 1.2. Перший застосунок на EF Core.
/// Класичний приклад Metanit: модель User, ApplicationContext, додавання та читання записів.
/// </summary>
public class Lesson_1_02_FirstApp : ILesson
{
    public string Id => "1.2";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Перший застосунок на EF Core";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.2.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 2;

    /// <summary>
    /// Запускає класичний перший сценарій EF Core: перестворення БД, додавання двох об'єктів
    /// User у таблицю Users, збереження через SaveChanges() та вибірка через ToList().
    /// </summary>
    public void Run()
    {
        var connectionString = SqlServerDbHelper.ConnectionString;

        // Створюємо контекст даних у блоці using, щоб після завершення роботи
        // звільнити некеровані ресурси (з'єднання з файлом SQLite)
        using (var db = new L1_2.ApplicationContext(connectionString))
        {
            // Гарантуємо чисту БД для демонстрації: видаляє старий файл БД (якщо є) і створює таблицю Users
            SqlServerDbHelper.RecreateSchema(db, "l1_02");

            SqlServerDbHelper.PrintSection("1. Створення та збереження об'єктів User");
            var tom = new L1_2.User { Name = "Tom", Age = 33 };
            var alice = new L1_2.User { Name = "Alice", Age = 26 };

            // Додаємо об'єкти до відстеження контекстом (Change Tracker переводить їх у стан EntityState.Added).
            // До виклику SaveChanges() властивість Id у tom та alice ще дорівнює 0.
            db.Users.Add(tom);
            db.Users.Add(alice);

            // Генеруємо та виконуємо SQL INSERT INTO "Users" ("Age", "Name") VALUES (@p0, @p1) RETURNING "Id"
            // у межах однієї неявної транзакції. Метод повертає кількість змінених рядків у БД.
            // Після SaveChanges() EF Core оновлює tom.Id та alice.Id значеннями з БД і переводить їх у стан Unchanged.
            int affected = db.SaveChanges();
            Console.WriteLine($"Об'єкти успішно збережено (змінено рядків у БД: {affected}).");

            SqlServerDbHelper.PrintSection("2. Отримання об'єктів із БД");
            // ToList() виконує SQL-запит SELECT "u"."Id", "u"."Age", "u"."Name" FROM "Users" AS "u"
            // та матеріалізує рядки таблиці у список C#-об'єктів.
            // Оскільки tom і alice вже відстежуються цим самим контекстом, Change Tracker (Identity Resolution)
            // поверне ті самі екземпляри об'єктів із пам'яті, не створюючи дублікатів.
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
    /// Модель користувача. Властивість Id за конвенцією іменування EF Core
    /// стає первинним ключем з автоінкрементом (INTEGER PRIMARY KEY AUTOINCREMENT у SQLite).
    /// </summary>
    public class User
    {
        /// <summary>
        /// Унікальний ідентифікатор користувача (первинний ключ).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача. Знак ? (nullable) означає, що стовпець у SQLite допускає NULL (TEXT NULL).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Вік користувача. Не-nullable тип int відображається як INTEGER NOT NULL.
        /// </summary>
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст даних застосунку — відповідає за сеанс роботи з базою даних.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("l1_02");
        private readonly string _connectionString;

        /// <summary>
        /// Колекція сутностей User, що відображається на таблицю "Users" у SQLite.
        /// Ініціалізація через Set&lt;User&gt;() дозволяє уникнути попереджень nullable-аналізатора (CS8618).
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Ініціалізує контекст із переданим рядком підключення.
        /// </summary>
        public ApplicationContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Конфігурує провайдер SQLite для цього контексту.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Для SQLite рядок підключення зазвичай має вигляд "Data Source=шлях_до_файлу.db"
            optionsBuilder.UseSqlServer(_connectionString);
        }
    }
}

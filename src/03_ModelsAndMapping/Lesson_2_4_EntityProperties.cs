using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.4: Властивості сутності.
/// Демонструє автоматичне зіставлення публічних властивостей, їх виключення через
/// [NotMapped] та Ignore(), а також властивості з явним backing field (get/set).
/// </summary>
public class Lesson_2_4_EntityProperties : ILesson
{
    public string Id => "2.4";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Властивості сутності";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.4.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 3;

    /// <summary>
    /// Запускає демонстрацію уроку: зберігає користувача, виводить DDL таблиці Users
    /// (щоб переконатися у відсутності виключених властивостей) та читає дані назад.
    /// </summary>
    public void Run()
    {
        SqliteDbHelper.PrintSection("2.4. Властивості сутності");

        using var db = new L2_4.ApplicationContext();
        SqliteDbHelper.RecreateDatabase(db);

        var user = new L2_4.User
        {
            Name = "Tom",
            Age = 35,
            Address = "Тимчасова адреса (не зберігається в БД)",
            SessionToken = "secret-token-123"
        };
        db.Users.Add(user);
        db.SaveChanges();

        // Перевіряємо, які стовпці реально створено в таблиці Users через системну таблицю SQLite.
        // У DDL мають бути лише Id, Age та Name (без Address, SessionToken та Summary).
        var createSql = db.Database
            .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Users'")
            .FirstOrDefault();

        Console.WriteLine("DDL таблиці Users (без Address і SessionToken):");
        Console.WriteLine(createSql);

        // Читаємо з БД у новому запиті без кешування (AsNoTracking), щоб гарантовано отримати
        // новий екземпляр User із БД (без залишкових значень Address/SessionToken з Change Tracker).
        var fromDb = db.Users.AsNoTracking().Single();
        Console.WriteLine($"\nПрочитано з БД: Id={fromDb.Id}, Name={fromDb.Name}, Age={fromDb.Age}");
        Console.WriteLine($"Виключені властивості після читання з БД: Address='{fromDb.Address ?? "null"}', SessionToken='{fromDb.SessionToken ?? "null"}'");
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_4
{
    /// <summary>
    /// Сутність користувача, яка демонструє різні види властивостей:
    /// звичайні get/set, властивість із backing field, виключені властивості та read-only обчислювану властивість.
    /// </summary>
    public class User
    {
        // Приватне поле (backing field), яке EF Core може розпізнати за угодою іменування (_name).
        private string _name = string.Empty;

        /// <summary>Первинний ключ користувача (INTEGER PRIMARY KEY AUTOINCREMENT у SQLite).</summary>
        public int Id { get; set; }

        /// <summary>
        /// Повноцінна властивість із логікою нормалізації в set-аксесорі (Trim).
        /// Важливо: за замовчуванням EF Core при читанні з БД записує значення напряму в backing field (_name),
        /// оминаючи set-аксесор, а при записі в БД читає через get-аксесор або поле залежно від PropertyAccessMode.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value.Trim();
        }

        /// <summary>Вік користувача (обов'язковий стовпець INTEGER NOT NULL).</summary>
        public int Age { get; set; }

        /// <summary>
        /// Адреса користувача. Виключена з моделі за допомогою Fluent API
        /// (див. OnModelCreating -> Ignore(u => u.Address)), тому стовпець у БД не створюється.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Токен сесії. Виключений з моделі за допомогою атрибута [NotMapped],
        /// тому існує лише в пам'яті C# і не відображається на таблицю Users.
        /// </summary>
        [NotMapped]
        public string? SessionToken { get; set; }

        /// <summary>
        /// Обчислювана властивість лише для читання (без set-аксесора).
        /// За умовними угодами EF Core не зіставляє зі стовпцями властивості без set.
        /// </summary>
        public string Summary => $"{Name} ({Age} р.)";
    }

    /// <summary>
    /// Контекст бази даних для уроку 2.4, який демонструє виключення властивості через Fluent API.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        /// <summary>Набір користувачів, що проєктується на таблицю "Users".</summary>
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_4"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Виключаємо властивість Address із моделі.
            // Fluent API має перевагу перед [NotMapped], коли потрібно не засмічувати доменну модель атрибутами.
            modelBuilder.Entity<User>().Ignore(u => u.Address);
        }
    }
}

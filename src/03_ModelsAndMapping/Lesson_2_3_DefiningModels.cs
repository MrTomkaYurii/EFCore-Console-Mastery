using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.3: Визначення моделей.
/// Показує, як сутності потрапляють у модель EF Core (DbSet, навігаційні властивості,
/// modelBuilder.Entity&lt;T&gt;()) і як виключати типи через Ignore&lt;T&gt;() або [NotMapped].
/// </summary>
public class Lesson_2_3_DefiningModels : ILesson
{
    public string Id => "2.3";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Визначення моделей";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.3.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 2;

    /// <summary>
    /// Запускає демонстрацію двох сценаріїв:
    /// 1) автоматичне та явне включення сутностей у модель (DbSet, навігація, Entity&lt;T&gt;());
    /// 2) виключення сутностей із моделі через [NotMapped] та modelBuilder.Ignore&lt;T&gt;().
    /// </summary>
    public void Run()
    {
        SqliteDbHelper.PrintSection("2.3. Визначення моделей");

        // Частина 1: Автоматичне включення через DbSet, навігаційні властивості та Entity<T>()
        using (var db = new L2_3.InclusionContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            var company = new L2_3.Company { Name = "TechCorp" };
            var user = new L2_3.User { Name = "Tom", Age = 30, Company = company };
            // Додавання user автоматично додає до Change Tracker і пов'язану сутність company (каскадне відстеження графу об'єктів).
            db.Users.Add(user);
            // Для Country немає DbSet<Country>, тому звертаємося через універсальний метод db.Set<Country>().
            db.Set<L2_3.Country>().Add(new L2_3.Country { Name = "Ukraine" });
            db.SaveChanges();

            // Запитуємо системну таблицю sqlite_master, виключаючи службові таблиці SQLite (напр., sqlite_sequence).
            var tables = db.Database
                .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
                .ToList();

            Console.WriteLine("Таблиці в InclusionContext (Users + Company через навігацію + Country через Entity<T>()):");
            foreach (var t in tables)
                Console.WriteLine($"  - {t}");
        }

        // Частина 2: Виключення сутностей через Ignore<T>() та [NotMapped]
        using (var db = new L2_3.ExclusionContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            db.Users.Add(new L2_3.UserWithExcluded { Name = "Alice", Age = 25 });
            db.SaveChanges();

            var tables = db.Database
                .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
                .ToList();

            Console.WriteLine("\nТаблиці в ExclusionContext (тільки Users, бо TempBadge має [NotMapped], а AuditLog виключено через Ignore):");
            foreach (var t in tables)
                Console.WriteLine($"  - {t}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_3
{
    /// <summary>
    /// Користувач, що входить у модель через DbSet&lt;User&gt; і тягне за собою Company.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача (nullable TEXT у SQLite).</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача (INTEGER NOT NULL у SQLite).</summary>
        public int Age { get; set; }

        /// <summary>
        /// Навігаційна властивість до компанії.
        /// Наявність цієї властивості автоматично включає тип Company у модель EF Core
        /// (створюється таблиця "Company" та тіньовий зовнішній ключ CompanyId у таблиці "Users"),
        /// навіть якщо у DbContext немає властивості DbSet&lt;Company&gt;.
        /// </summary>
        public Company? Company { get; set; }
    }

    /// <summary>
    /// Компанія, включена до моделі транзитивно через навігаційну властивість User.Company.
    /// Зверніть увагу: таблиця в SQLite отримає ім'я "Company" (в однині), а не "Companies",
    /// бо для неї не оголошено DbSet&lt;Company&gt; (EF Core бере ім'я класу, якщо немає DbSet).
    /// </summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Країна, включена до моделі явно через modelBuilder.Entity&lt;Country&gt;().
    /// Як і для Company, таблиця в БД називатиметься "Country" (за іменем класу).
    /// </summary>
    public class Country
    {
        /// <summary>Первинний ключ країни.</summary>
        public int Id { get; set; }

        /// <summary>Назва країни.</summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Контекст, що демонструє три способи включення сутностей у модель EF Core:
    /// DbSet&lt;T&gt;, навігаційна властивість та виклик modelBuilder.Entity&lt;T&gt;().
    /// </summary>
    public class InclusionContext : DbContext
    {
        /// <summary>
        /// Набір користувачів. Ініціалізація через "= null!;" (null-forgiving operator)
        /// прибирає попередження компілятора CS8618 (EF Core ініціалізує DbSet через reflection).
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_3_inc"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Явне включення сутності Country без оголошення DbSet<Country>.
            // Зручно, коли довідник потрібен у схемі БД, але до нього рідко звертаються напряму.
            modelBuilder.Entity<Country>();
        }
    }

    /// <summary>
    /// Допоміжний клас із атрибутом [NotMapped].
    /// EF Core повністю ігнорує цей тип при побудові моделі, навіть якщо інша сутність має на нього посилання.
    /// </summary>
    [NotMapped]
    public class TempBadge
    {
        /// <summary>Тимчасовий код перепустки (не зберігається в БД).</summary>
        public int Code { get; set; }
    }

    /// <summary>
    /// Клас журналу аудиту, який виключено з моделі за допомогою Fluent API (modelBuilder.Ignore&lt;AuditLog&gt;()).
    /// На відміну від [NotMapped], сам клас залишається "чистим" від атрибутів EF/DataAnnotations.
    /// </summary>
    public class AuditLog
    {
        /// <summary>Ідентифікатор запису (у цій моделі не відображається на БД).</summary>
        public int Id { get; set; }

        /// <summary>Текст повідомлення аудиту.</summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// Сутність користувача, що містить посилання на виключені типи TempBadge та AuditLog.
    /// </summary>
    public class UserWithExcluded
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }

        /// <summary>
        /// Навігаційна властивість, яка не створить таблицю чи зовнішній ключ у БД,
        /// оскільки тип TempBadge позначений атрибутом [NotMapped].
        /// </summary>
        public TempBadge? Badge { get; set; }

        /// <summary>
        /// Навігаційна властивість, яка не створить таблицю чи зовнішній ключ у БД,
        /// оскільки тип AuditLog виключено через modelBuilder.Ignore&lt;AuditLog&gt;().
        /// </summary>
        public AuditLog? LastAudit { get; set; }
    }

    /// <summary>
    /// Контекст, що демонструє виключення типів із моделі EF Core.
    /// </summary>
    public class ExclusionContext : DbContext
    {
        /// <summary>
        /// Альтернативний спосіб оголошення DbSet без "= null!;": expression-bodied властивість через Set&lt;T&gt;().
        /// Таблиця в SQLite отримає назву "Users" за іменем цієї властивості DbSet.
        /// </summary>
        public DbSet<UserWithExcluded> Users => Set<UserWithExcluded>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_3_exc"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Виключаємо тип AuditLog із моделі на рівні Fluent API.
            // Це має вищий пріоритет і не вимагає модифікації коду самого класу AuditLog.
            modelBuilder.Ignore<AuditLog>();
        }
    }
}

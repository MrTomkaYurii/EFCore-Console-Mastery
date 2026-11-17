using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.17: Налаштування індексів.
/// Демонструє створення звичайних, унікальних, складених та фільтрованих індексів
/// за допомогою атрибута [Index] та Fluent API (HasIndex, IsUnique, HasDatabaseName, HasFilter).
/// </summary>
public class Lesson_2_17_Indexes : ILesson
{
    public string Id => "2.17";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Налаштування індексів";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.17.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 9;

    /// <summary>
    /// Запускає демонстрацію індексів у SQLite:
    /// 1) виводить усі згенеровані CREATE [UNIQUE] INDEX із системної таблиці sqlite_master;
    /// 2) показує, що фільтрований унікальний індекс (HasFilter) дозволяє кілька значень NULL;
    /// 3) перевіряє блокування дубліката за унікальним індексом Passport.
    /// </summary>
    public void Run()
    {
        SqliteDbHelper.PrintSection("2.17. Налаштування індексів");

        using var db = new L2_17.ApplicationContext();
        SqliteDbHelper.RecreateDatabase(db);

        // Переглядаємо всі створені індекси в SQLite через sqlite_master.
        // Умова sql IS NOT NULL відфільтровує автоматичні внутрішні індекси SQLite (sqlite_autoindex_*).
        var indexes = db.Database
            .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='index' AND sql IS NOT NULL ORDER BY name")
            .ToList();

        Console.WriteLine("Створені індекси в SQLite:");
        foreach (var idxSql in indexes)
        {
            Console.WriteLine($"  {idxSql}");
        }

        // Додаємо записи: PhoneNumber може бути NULL у кількох користувачів завдяки фільтрованому унікальному індексу.
        // Примітка: у SQLite звичайний UNIQUE-індекс також дозволяє кілька NULL (за стандартом SQL NULL != NULL),
        // але в MS SQL Server без HasFilter("[PhoneNumber] IS NOT NULL") другий NULL спричинив би помилку!
        // Крім того, фільтрований (partial) індекс у SQLite менший за розміром, бо не індексує NULL-рядки.
        db.Users.AddRange(
            new L2_17.User { Name = "Tom", Passport = "P-100", PhoneNumber = "+380501112233" },
            new L2_17.User { Name = "Alice", Passport = "P-200", PhoneNumber = null },
            new L2_17.User { Name = "Bob", Passport = "P-300", PhoneNumber = null }
        );
        db.SaveChanges();
        Console.WriteLine("\nУспішно додано двох користувачів з PhoneNumber = NULL (фільтрований індекс не блокує NULL).");

        // Перевірка унікальності індексу на Passport
        try
        {
            db.Users.Add(new L2_17.User { Name = "Clone", Passport = "P-100", PhoneNumber = "+380509998877" });
            // SQLite перевіряє UNIQUE INDEX IX_Users_Passport_Unique і відхиляє дублікат "P-100".
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"\nДубль за унікальним індексом Passport заблоковано:\n  {ex.InnerException?.Message}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_17
{
    /// <summary>
    /// Сутність користувача з індексами, заданими через атрибути [Index] (з простору Microsoft.EntityFrameworkCore):
    /// 1) звичайний індекс на PhoneNumber із власним іменем "Attr_Phone_Index";
    /// 2) складений індекс на (PhoneNumber, Passport) зі стандартним іменем IX_Users_PhoneNumber_Passport.
    /// </summary>
    [Index(nameof(PhoneNumber), Name = "Attr_Phone_Index")]
    [Index(nameof(PhoneNumber), nameof(Passport))] // Складений індекс через атрибут (порядок стовпців має значення для B-Tree!)
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача (без індексу).</summary>
        public string? Name { get; set; }

        /// <summary>Номер паспорта (має унікальний індекс через Fluent API).</summary>
        public string? Passport { get; set; }

        /// <summary>Номер телефону (має звичайний індекс через [Index] та фільтрований унікальний через Fluent API).</summary>
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// Контекст бази даних для уроку 2.17, що налаштовує унікальний та фільтрований індекси через Fluent API.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        /// <summary>Набір користувачів (таблиця "Users").</summary>
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_17"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Унікальний індекс на Passport через Fluent API.
            // Генерує: CREATE UNIQUE INDEX "IX_Users_Passport_Unique" ON "Users" ("Passport").
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Passport)
                .IsUnique()
                .HasDatabaseName("IX_Users_Passport_Unique");

            // Фільтрований (частковий / partial) унікальний індекс у SQLite:
            // індексує та перевіряє унікальність лише для непорожніх номерів телефону.
            // Зверніть увагу на синтаксис ідентифікаторів у HasFilter: подвійні лапки "" для SQLite/PostgreSQL
            // (у MS SQL Server використовуються квадратні дужки [PhoneNumber] IS NOT NULL).
            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique()
                .HasDatabaseName("IX_Users_Phone_NotNull")
                .HasFilter("\"PhoneNumber\" IS NOT NULL");
        }
    }
}

using System.ComponentModel.DataAnnotations;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 3.11: Обмеження властивостей.
/// Демонструє CHECK-обмеження (HasCheckConstraint) та обмеження довжини рядків
/// ([MaxLength], HasMaxLength), а також пояснює їхню поведінку в SQLite.
/// </summary>
public class Lesson_3_11_PropertyConstraints : ILesson
{
    public string Id => "3.11";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Обмеження властивостей";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.9.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 11;

    /// <summary>
    /// Запускає демонстрацію обмежень властивостей:
    /// 1) виводить DDL таблиці Users з CHECK-обмеженням "ValidAge";
    /// 2) перевіряє збереження валідного запису та блокування невалідного віку на рівні БД;
    /// 3) демонструє особливість SQLite: ігнорування обмеження довжини [MaxLength] / HasMaxLength.
    /// </summary>
    public void Run()
    {
        SqlServerDbHelper.PrintSection("3.11. Обмеження властивостей");

        using var db = new L2_9.ApplicationContext();
        SqlServerDbHelper.RecreateSchema(db, "l3_11");

        var sql = db.Database.GenerateCreateScript();

        Console.WriteLine("DDL схеми в MS SQL Server:");
        Console.WriteLine(sql);

        // 1. Успішне додавання валідного користувача
        db.Users.Add(new L2_9.User { Name = "Tom", Email = "tom@example.com", Age = 30 });
        db.SaveChanges();
        Console.WriteLine("\nКористувача Tom (Age=30) успішно збережено.");

        // 2. Порушення CHECK-обмеження ValidAge (Age > 0 AND Age < 120)
        try
        {
            db.Users.Add(new L2_9.User { Name = "Bob", Email = "bob@example.com", Age = 123 });
            // SQLite перевіряє CONSTRAINT "ValidAge" CHECK (Age > 0 AND Age < 120) під час INSERT і кидає SqliteException (обгорнуте в DbUpdateException).
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"\nОчікувана помилка при Age=123 (порушення CHECK \"ValidAge\"):\n  {ex.InnerException?.Message}");
        }

        // Від'єднуємо невалідного Bob (очищаємо ChangeTracker), щоб наступний SaveChanges() не намагався зберегти його знову.
        db.ChangeTracker.Clear();

        // 3. Демонстрація MaxLength у SQLite: SQLite ігнорує довжину TEXT і зберігає рядок > 10 символів
        // (на відміну від MS SQL Server, де nvarchar(10) спричинив би помилку "String or binary data would be truncated").
        var longName = "ДужеДовгеІм'яПонадДесятьСимволів";
        db.Users.Add(new L2_9.User { Name = longName, Email = "long@example.com", Age = 25 });
        db.SaveChanges();
        Console.WriteLine($"\nУвага (особливість SQLite): рядок довжиною {longName.Length} символів збережено попри [MaxLength(10)]!");
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_9
{
    /// <summary>
    /// Сутність користувача з обмеженнями довжини рядків та CHECK-обмеженням на вік.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача з атрибутом [MaxLength(10)].
        /// У MS SQL Server це створить стовпець nvarchar(10), але у SQLite тип усе одно буде TEXT
        /// без примусового обмеження довжини. Водночас [MaxLength] використовується валідацією (напр., ASP.NET / DataAnnotations).
        /// </summary>
        [MaxLength(10)]
        public string? Name { get; set; }

        /// <summary>
        /// Електронна пошта; максимальна довжина 50 символів задана через Fluent API HasMaxLength(50).
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Вік користувача, обмежений на рівні БД через CHECK-обмеження "ValidAge" (Age &gt; 0 AND Age &lt; 120).
        /// </summary>
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст бази даних для уроку 2.9, що налаштовує CHECK-обмеження та HasMaxLength.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        /// <summary>Набір користувачів (таблиця "Users").</summary>
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l3_11");
            // Обмеження CHECK на рівні таблиці (працює і в SQLite, і в MS SQL Server).
            // Додає в DDL: CONSTRAINT "ValidAge" CHECK (Age > 0 AND Age < 120).
            modelBuilder.Entity<User>()
                .ToTable(t => t.HasCheckConstraint("ValidAge", "Age > 0 AND Age < 120"));

            // Fluent API еквівалент [MaxLength(50)].
            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(50);
        }
    }
}

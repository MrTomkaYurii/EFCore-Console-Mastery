using System.ComponentModel.DataAnnotations;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 3.8: Налаштування ключів.
/// Демонструє первинні ключі за угодою, [Key], HasKey/HasName,
/// складені первинні ключі та альтернативні (унікальні) ключі HasAlternateKey.
/// </summary>
public class Lesson_3_08_Keys : ILesson
{
    public string Id => "3.8";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Налаштування ключів";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.7.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 8;

    /// <summary>
    /// Запускає демонстрацію налаштування ключів:
    /// 1) простий первинний ключ із власним іменем (HasKey + HasName) та альтернативний ключ (HasAlternateKey);
    /// 2) перевірка обмеження UNIQUE на рівні SQLite при спробі дублювання альтернативного ключа;
    /// 3) складений первинний ключ із двох стовпців (PassportSeria + PassportNumber).
    /// </summary>
    public void Run()
    {
        SqlServerDbHelper.PrintSection("3.8. Налаштування ключів");

        // 1. Первинний ключ через Fluent API (HasKey + HasName) та альтернативний ключ (HasAlternateKey)
        using (var db = new L2_7.SingleAndAlternateKeyContext())
        {
            SqlServerDbHelper.RecreateSchema(db, "l3_08");

            var sql = db.Database.GenerateCreateScript();

            Console.WriteLine("1) DDL таблиці Users (HasKey + HasName + HasAlternateKey):");
            Console.WriteLine(sql);

            db.Users.Add(new L2_7.User { Name = "Tom", Passport = "AB123456" });
            db.SaveChanges();

            // Очищаємо ChangeTracker, щоб перевірити саме обмеження UNIQUE на рівні БД
            // (інакше EF Core міг би відстежувати конфлікт ключів ще в пам'яті або тримати старий стан).
            db.ChangeTracker.Clear();

            // Перевірка унікальності альтернативного ключа Passport
            try
            {
                db.Users.Add(new L2_7.User { Name = "Bob", Passport = "AB123456" });
                // SQLite відхилить INSERT через порушення UNIQUE constraint на стовпці Passport.
                db.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"\nСпроба дублювання альтернативного ключа Passport заблокована БД:\n  {ex.InnerException?.Message}");
            }
        }

        // 2. Складений первинний ключ (лише через Fluent API)
        using (var db = new L2_7.CompositeKeyContext())
        {
            SqlServerDbHelper.RecreateSchema(db, "l3_08");

            var sql = db.Database.GenerateCreateScript();

            Console.WriteLine("\n2) DDL таблиці Citizens (складений первинний ключ):");
            Console.WriteLine(sql);

            // У таблиці Citizens обидва записи валідні: однакова серія "1234", але різні номери.
            db.Citizens.AddRange(
                new L2_7.Citizen { PassportSeria = "1234", PassportNumber = "345678", Name = "Tom" },
                new L2_7.Citizen { PassportSeria = "1234", PassportNumber = "345679", Name = "Bob" }
            );
            db.SaveChanges();

            Console.WriteLine("Збережені громадяни зі складеним ключем:");
            foreach (var c in db.Citizens.AsNoTracking().ToList())
            {
                Console.WriteLine($"  {c.Name}: {c.PassportSeria} {c.PassportNumber}");
            }
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_7
{
    /// <summary>
    /// Сутність користувача з нестандартним іменем первинного ключа (Ident замість Id/UserId)
    /// та альтернативним унікальним ключем Passport.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Нестандартне ім'я первинного ключа: EF Core не розпізнає Ident за угодою,
        /// тому в SingleAndAlternateKeyContext його налаштовано явно через HasKey(u => u.Ident).
        /// </summary>
        public int Ident { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>
        /// Альтернативний ключ (отримає обмеження UNIQUE у БД через HasAlternateKey).
        /// Також робить властивість обов'язковою (NOT NULL) у моделі EF Core, навіть якщо тип string?.
        /// </summary>
        public string? Passport { get; set; }
    }

    /// <summary>
    /// Товар із первинним ключем, заданим через Data Annotation [Key].
    /// </summary>
    public class AnnotatedProduct
    {
        /// <summary>
        /// Первинний ключ Code, позначений атрибутом [Key] (альтернатива Fluent API HasKey).
        /// Оскільки тип int, у SQLite він автоматично отримає AUTOINCREMENT.
        /// </summary>
        [Key]
        public int Code { get; set; }

        /// <summary>Назва товару.</summary>
        public string? Title { get; set; }
    }

    /// <summary>
    /// Контекст для демонстрації одиничного первинного ключа, іменування обмеження PK та альтернативного ключа.
    /// </summary>
    public class SingleAndAlternateKeyContext : DbContext
    {
        /// <summary>Набір користувачів (таблиця "Users").</summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>Набір товарів (таблиця "Products").</summary>
        public DbSet<AnnotatedProduct> Products => Set<AnnotatedProduct>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l3_08");
            // Вказуємо Ident як первинний ключ і задаємо власне ім'я обмеження PRIMARY KEY ("UsersPrimaryKey")
            // замість стандартного "PK_Users".
            modelBuilder.Entity<User>()
                .HasKey(u => u.Ident)
                .HasName("UsersPrimaryKey");

            // HasAlternateKey створює UNIQUE-обмеження (AK_Users_Passport) у БД
            // і дозволяє використовувати Passport як ціль для зовнішніх ключів (HasPrincipalKey).
            modelBuilder.Entity<User>()
                .HasAlternateKey(u => u.Passport);
        }
    }

    /// <summary>
    /// Сутність громадянина зі складеним первинним ключем (PassportSeria + PassportNumber).
    /// </summary>
    public class Citizen
    {
        /// <summary>Серія паспорта (перша частина складеного первинного ключа).</summary>
        public string PassportSeria { get; set; } = string.Empty;

        /// <summary>Номер паспорта (друга частина складеного первинного ключа).</summary>
        public string PassportNumber { get; set; } = string.Empty;

        /// <summary>Ім'я громадянина.</summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Контекст для демонстрації складеного первинного ключа.
    /// </summary>
    public class CompositeKeyContext : DbContext
    {
        /// <summary>Набір громадян (таблиця "Citizens").</summary>
        public DbSet<Citizen> Citizens => Set<Citizen>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l3_08");
            // Складений первинний ключ можна задати тільки через Fluent API (анонімний тип у HasKey).
            // Примітка: у новіших версіях EF Core існує атрибут [PrimaryKey(nameof(...), nameof(...))],
            // але класичний підхід — саме Fluent API. Для складеного ключа автогенерація не працює.
            modelBuilder.Entity<Citizen>()
                .HasKey(c => new { c.PassportSeria, c.PassportNumber });
        }
    }
}

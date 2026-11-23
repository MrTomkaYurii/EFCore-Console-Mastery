using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 3.10: Генерація значень властивостей і стовпців.
/// Демонструє автогенерацію ключів, відключення автогенерації ([DatabaseGenerated(None)] / ValueGeneratedNever),
/// значення за замовчуванням (HasDefaultValue, HasDefaultValueSql) та обчислювані стовпці (HasComputedColumnSql).
/// </summary>
public class Lesson_3_10_ValueGeneration : ILesson
{
    public string Id => "3.10";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Генерація значень властивостей і стовпців";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.8.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 10;

    /// <summary>
    /// Запускає демонстрацію генерації значень у БД:
    /// 1) значення за замовчуванням (HasDefaultValue, HasDefaultValueSql) та обчислюваний стовпець (HasComputedColumnSql);
    /// 2) відключення автогенерації первинного ключа через [DatabaseGenerated(None)] та ValueGeneratedNever().
    /// </summary>
    public void Run()
    {
        SqliteDbHelper.PrintSection("3.10. Генерація значень властивостей і стовпців");

        // Частина 1: HasDefaultValue, HasDefaultValueSql("DATETIME('now')") та HasComputedColumnSql
        using (var db = new L2_8.DefaultsAndComputedContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            var sql = db.Database
                .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Users'")
                .FirstOrDefault();

            Console.WriteLine("1) DDL таблиці Users (DEFAULT 18, DEFAULT DATETIME('now'), GENERATED ALWAYS AS):");
            Console.WriteLine(sql);

            var user = new L2_8.User
            {
                FirstName = "Tom",
                LastName = "Smith"
                // Age та CreatedAt не задаємо — спрацюють значення за замовчуванням у БД.
                // Увага: для int Age за замовчуванням у C# дорівнює 0 (CLR default), тому EF Core
                // не передає Age у INSERT і дозволяє БД підставити DEFAULT 18.
            };

            Console.WriteLine($"\nДо SaveChanges: Id={user.Id}, Age={user.Age}, CreatedAt={user.CreatedAt}, FullName='{user.FullName ?? "null"}'");
            db.Users.Add(user);
            // SaveChanges() виконує INSERT ... RETURNING "Id", "Age", "CreatedAt", "FullName",
            // тому всі згенеровані БД значення одразу записуються назад у C#-об'єкт user.
            db.SaveChanges();
            Console.WriteLine($"Після SaveChanges: Id={user.Id}, Age={user.Age}, CreatedAt={user.CreatedAt:yyyy-MM-dd HH:mm:ss}, FullName='{user.FullName}'");
        }

        // Частина 2: Відключення автогенерації ключа через ValueGeneratedNever() та [DatabaseGenerated(None)]
        using (var db = new L2_8.ManualKeyContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            // Оскільки автогенерацію вимкнено, ми зобов'язані передати унікальні Id вручну.
            db.ManualUsers.AddRange(
                new L2_8.ManualUser { Id = 101, Name = "Alice" },
                new L2_8.ManualUser { Id = 205, Name = "Bob" }
            );
            db.SaveChanges();

            Console.WriteLine("\n2) Користувачі з ключами, заданими вручну (ValueGeneratedNever):");
            foreach (var u in db.ManualUsers.AsNoTracking().ToList())
            {
                Console.WriteLine($"  Id={u.Id}, Name={u.Name}");
            }
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_8
{
    /// <summary>
    /// Сутність користувача, що демонструє автогенерований ключ, значення за замовчуванням та обчислюваний стовпець.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ із автогенерацією (INTEGER PRIMARY KEY AUTOINCREMENT у SQLite).</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? FirstName { get; set; }

        /// <summary>Прізвище користувача.</summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Обчислюваний стовпець у БД (лише get у C#, без set).
        /// SQLite обчислює його через GENERATED ALWAYS AS (FirstName || ' ' || LastName) VIRTUAL.
        /// </summary>
        public string? FullName { get; }

        /// <summary>Вік із фіксованим значенням за замовчуванням (18) на рівні БД.</summary>
        public int Age { get; set; }

        /// <summary>Дата/час створення запису, що генерується SQLite через DATETIME('now').</summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Контекст для демонстрації HasDefaultValue, HasDefaultValueSql та HasComputedColumnSql у SQLite.
    /// </summary>
    public class DefaultsAndComputedContext : DbContext
    {
        /// <summary>Набір користувачів (таблиця "Users").</summary>
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_8_def"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Фіксоване значення за замовчуванням: у DDL створює DEFAULT 18.
            modelBuilder.Entity<User>()
                .Property(u => u.Age)
                .HasDefaultValue(18);

            // SQL-вираз для значення за замовчуванням у SQLite (замість GETDATE() / SYSDATETIME() у MS SQL Server).
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("DATETIME('now')");

            // Обчислюваний стовпець у синтаксисі SQLite (конкатенація через || замість + у T-SQL).
            // За замовчуванням у SQLite 3.31+ створюється VIRTUAL-стовпець (обчислюється при читанні, не займає місце на диску).
            modelBuilder.Entity<User>()
                .Property(u => u.FullName)
                .HasComputedColumnSql("FirstName || ' ' || LastName");
        }
    }

    /// <summary>
    /// Сутність із первинним ключем, для якого вимкнено автогенерацію в БД.
    /// </summary>
    public class ManualUser
    {
        /// <summary>
        /// Первинний ключ без автогенерації (позначено [DatabaseGenerated(DatabaseGeneratedOption.None)]).
        /// У DDL SQLite такий стовпець усе одно матиме INTEGER PRIMARY KEY, але без AUTOINCREMENT,
        /// а EF Core буде явно передавати значення Id у INSERT замість очікування значення від БД.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Контекст для демонстрації ручного керування первинними ключами (ValueGeneratedNever).
    /// </summary>
    public class ManualKeyContext : DbContext
    {
        /// <summary>Набір користувачів із ручними ключами (таблиця "ManualUsers").</summary>
        public DbSet<ManualUser> ManualUsers => Set<ManualUser>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_8_man"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Дублюємо через Fluent API (ValueGeneratedNever) для наочності:
            // Fluent API має вищий пріоритет за Data Annotations і є еквівалентом [DatabaseGenerated(None)].
            modelBuilder.Entity<ManualUser>()
                .Property(u => u.Id)
                .ValueGeneratedNever();
        }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 3.1: Моделі, Fluent API та анотації даних.
/// Демонструє три підходи до конфігурації моделі в EF Core:
/// умовні угоди (conventions), анотації даних (Data Annotations) та Fluent API.
/// </summary>
public class Lesson_3_01_ModelsAndFluentApi : ILesson
{
    public string Id => "3.1";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Моделі, Fluent API та анотації даних";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.1.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 1;

    /// <summary>
    /// Запускає демонстрацію уроку: створює БД, додає користувачів,
    /// виводить згенерований SQLite DDL-скрипт таблиці та читає дані без відстеження.
    /// </summary>
    public void Run()
    {
        SqliteDbHelper.PrintSection("3.1. Моделі, Fluent API та анотації даних");

        using var db = new L2_1.ApplicationContext();
        // RecreateDatabase перестворює чисту БД SQLite (EnsureDeleted + EnsureCreated),
        // щоб демонстрація щоразу починалася з порожньої схеми без міграцій.
        SqliteDbHelper.RecreateDatabase(db);

        // Додаємо користувачів: для Id спрацьовує автогенерація (PRIMARY KEY AUTOINCREMENT у SQLite),
        // а відображення на стовпець таблиці налаштоване через [Column("user_id")].
        db.Users.AddRange(
            new L2_1.User { Name = "Tom", Age = 33 },
            new L2_1.User { Name = "Alice", Age = 26 }
        );
        // SaveChanges() генерує пакетний INSERT INTO "Users" ("Age", "Name") VALUES ... RETURNING "user_id".
        db.SaveChanges();

        // Перевіряємо згенеровану схему таблиці Users безпосередньо в системній таблиці SQLite (sqlite_master).
        // Псевдонім AS Value потрібен для SqlQueryRaw<string>, щоб EF Core зіставив єдиний стовпець зі скаляром.
        var createSql = db.Database
            .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Users'")
            .FirstOrDefault();

        Console.WriteLine("DDL таблиці Users у SQLite:");
        Console.WriteLine(createSql);

        Console.WriteLine("\nЗбережені користувачі:");
        // AsNoTracking() вимикає Change Tracker для запиту: об'єкти не кешуються і не відстежуються,
        // що зменшує споживання пам'яті та прискорює read-only сценарії.
        foreach (var user in db.Users.AsNoTracking().ToList())
        {
            Console.WriteLine($"  user_id={user.Id}, Name={user.Name}, Age={user.Age}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_1
{
    /// <summary>
    /// Сутність User, де поєднано умовні угоди (conventions для Name, Age, назви таблиці Users)
    /// та анотацію даних [Column("user_id")] для властивості Id.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Первинний ключ сутності (за угодою EF Core розпізнає властивість Id як PK).
        /// Атрибут [Column("user_id")] змінює лише ім'я стовпця в БД, не змінюючи імені властивості в C#.
        /// </summary>
        [Column("user_id")]
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача. Nullable string? за угодою створює стовпець TEXT (допускає NULL),
        /// але у Fluent API для нього задано значення за замовчуванням "Anonymous".
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Вік користувача. Non-nullable int за угодою створює стовпець INTEGER NOT NULL.
        /// </summary>
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст бази даних для уроку 2.1, що демонструє пріоритет конфігурації:
    /// Fluent API (OnModelCreating) > Data Annotations > Умовні угоди (Conventions).
    /// </summary>
    public class ApplicationContext : DbContext
    {
        /// <summary>
        /// Набір сутностей Users, який за угодою проєктується на таблицю "Users" у SQLite.
        /// </summary>
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Налаштовуємо провайдер SQLite з окремим файлом БД для цього уроку.
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_1"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Точка входу для Fluent API: тут можна перевизначити угоди та анотації даних.
            // HasDefaultValue("Anonymous") додає в DDL SQLite клаузулу DEFAULT 'Anonymous'
            // (на відміну від MS SQL Server, де може генеруватися іменоване DEFAULT-обмеження DF_...).
            modelBuilder.Entity<User>()
                .Property(u => u.Name)
                .HasDefaultValue("Anonymous");

            base.OnModelCreating(modelBuilder);
        }
    }
}

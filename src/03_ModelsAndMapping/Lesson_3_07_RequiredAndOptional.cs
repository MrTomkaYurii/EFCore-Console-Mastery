using System.ComponentModel.DataAnnotations;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 3.7: Обов'язкові та необов'язкові властивості.
/// Показує, як nullable-типи в C#, атрибут [Required] та метод IsRequired()
/// впливають на обмеження NOT NULL у таблицях бази даних.
/// </summary>
public class Lesson_3_07_RequiredAndOptional : ILesson
{
    public string Id => "3.7";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Обов'язкові та необов'язкові властивості";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.15.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 7;

    /// <summary>
    /// Запускає демонстрацію обов'язкових (NOT NULL) та необов'язкових (NULL) властивостей:
    /// виводить DDL таблиці Users, зберігає валідний запис і перевіряє помилку БД при Email = null.
    /// </summary>
    public void Run()
    {
        SqlServerDbHelper.PrintSection("3.7. Обов'язкові та необов'язкові властивості");

        using var db = new L2_15.ApplicationContext();
        SqlServerDbHelper.RecreateSchema(db, "l3_07");

        var sql = db.Database.GenerateCreateScript();

        Console.WriteLine("DDL схеми в MS SQL Server:");
        Console.WriteLine(sql);

        // Коректне додавання користувача з усіма обов'язковими полями
        db.Users.Add(new L2_15.User
        {
            Name = "Tom",
            Email = "tom@example.com",
            Login = "tom_dev",
            Company = null, // Необов'язкова властивість (стовпець допускає NULL)
            Age = 28
        });
        db.SaveChanges();
        Console.WriteLine("\nКористувача Tom успішно збережено (Company = NULL дозволено).");

        // Спроба зберегти користувача без обов'язкової властивості Email ([Required])
        try
        {
            db.Users.Add(new L2_15.User
            {
                Name = "Bob",
                Email = null,   // Порушує [Required] -> NOT NULL constraint failed у SQLite
                Login = "bob"
            });
            // Увага: у консольному EF Core SaveChanges() не виконує DataAnnotations-валідацію на клієнті,
            // тому запит іде в SQLite, який і кидає SqliteException (NOT NULL constraint failed: Users.Email).
            db.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"\nОчікувана помилка DbUpdateException при Email = null:\n  {ex.InnerException?.Message ?? ex.Message}");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_15
{
    /// <summary>
    /// Сутність користувача, що демонструє всі способи задання обов'язковості (NOT NULL) у EF Core.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ (завжди NOT NULL).</summary>
        public int Id { get; set; }

        /// <summary>
        /// Не-nullable посилальний тип (NRT у C# з увімкненим &lt;Nullable&gt;enable&lt;/Nullable&gt;) ->
        /// EF Core за угодою створює стовпець TEXT NOT NULL у SQLite.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Nullable-тип (string?), але з атрибутом [Required] -> стає NOT NULL у БД.
        /// Зручно, коли в C# об'єкт може створюватися поетапно, але в БД поле обов'язкове.
        /// </summary>
        [Required]
        public string? Email { get; set; }

        /// <summary>
        /// Nullable-тип (string?), налаштований як обов'язковий через Fluent API IsRequired() у OnModelCreating.
        /// </summary>
        public string? Login { get; set; }

        /// <summary>
        /// Nullable-тип без обмежень -> допускає NULL у БД (стовпець TEXT без NOT NULL).
        /// </summary>
        public string? Company { get; set; }

        /// <summary>
        /// Значимий тип int (не int?) -> завжди NOT NULL у БД (для nullable потрібен int?).
        /// </summary>
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст бази даних для уроку 2.15.
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
            modelBuilder.HasDefaultSchema("l3_07");
            // Позначаємо Login як обов'язковий (NOT NULL) через Fluent API.
            // Зворотна дія (зробити не-nullable поле необов'язковим) виконується через IsRequired(false).
            modelBuilder.Entity<User>()
                .Property(u => u.Login)
                .IsRequired();
        }
    }
}

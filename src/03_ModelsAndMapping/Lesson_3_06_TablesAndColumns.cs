using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 3.6: Зіставлення таблиць і стовпців.
/// Демонструє перевизначення імен таблиць ([Table], ToTable)
/// та імен стовпців ([Column], HasColumnName).
/// </summary>
public class Lesson_3_06_TablesAndColumns : ILesson
{
    public string Id => "3.6";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Зіставлення таблиць і стовпців";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.6.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 6;

    /// <summary>
    /// Запускає демонстрацію двох способів перевизначення імен таблиць і стовпців:
    /// через Data Annotations ([Table], [Column]) та через Fluent API (ToTable, HasColumnName).
    /// </summary>
    public void Run()
    {
        SqliteDbHelper.PrintSection("3.6. Зіставлення таблиць і стовпців");

        // Приклад 1: Анотації даних [Table] та [Column]
        using (var db = new L2_6.AnnotationsContext())
        {
            SqliteDbHelper.RecreateDatabase(db);
            db.Users.Add(new L2_6.UserAnnotated { Name = "Tom" });
            db.SaveChanges();

            // Читаємо DDL з sqlite_master саме для таблиці 'People', а не 'Users'
            var sql = db.Database
                .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='People'")
                .FirstOrDefault();

            Console.WriteLine("1) Анотації [Table(\"People\")] і [Column(\"user_id\")]:");
            Console.WriteLine(sql);
        }

        // Приклад 2: Fluent API ToTable та HasColumnName
        using (var db = new L2_6.FluentContext())
        {
            SqliteDbHelper.RecreateDatabase(db);
            db.Users.Add(new L2_6.UserFluent { Name = "Alice" });
            db.SaveChanges();

            var sql = db.Database
                .SqlQueryRaw<string>("SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='Employees'")
                .FirstOrDefault();

            Console.WriteLine("\n2) Fluent API ToTable(\"Employees\", schema: \"userstore\") і HasColumnName(\"employee_id\"):");
            Console.WriteLine(sql);
            Console.WriteLine("   Примітка: SQLite не підтримує схеми (schema), тому таблиця створюється просто як \"Employees\".");
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_6
{
    /// <summary>
    /// Сутність, ім'я таблиці та стовпців якої задано через Data Annotations.
    /// Атрибут [Table("People")] перевизначає стандартне ім'я таблиці "Users" (від DbSet).
    /// </summary>
    [Table("People")]
    public class UserAnnotated
    {
        /// <summary>
        /// Первинний ключ у C# називається Id, але в БД буде створено стовпець "user_id".
        /// </summary>
        [Column("user_id")]
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача; у БД відображається на стовпець "full_name".
        /// </summary>
        [Column("full_name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Контекст для демонстрації зіставлення таблиць і стовпців через атрибути [Table] / [Column].
    /// </summary>
    public class AnnotationsContext : DbContext
    {
        /// <summary>Набір користувачів (у БД це таблиця "People", а не "Users").</summary>
        public DbSet<UserAnnotated> Users => Set<UserAnnotated>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_6_ann"));
        }
    }

    /// <summary>
    /// "Чиста" (POCO) сутність без жодних атрибутів — уся конфігурація винесена у Fluent API.
    /// </summary>
    public class UserFluent
    {
        /// <summary>Первинний ключ (у БД буде перейменований на "employee_id" через HasColumnName).</summary>
        public int Id { get; set; }

        /// <summary>Ім'я співробітника (залишається зі стандартним ім'ям стовпця "Name").</summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Контекст для демонстрації Fluent API методів ToTable() та HasColumnName().
    /// </summary>
    public class FluentContext : DbContext
    {
        /// <summary>Набір співробітників (у БД це таблиця "Employees").</summary>
        public DbSet<UserFluent> Users => Set<UserFluent>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_6_fl"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ToTable("Employees", schema: "userstore") вказує ім'я таблиці та схему.
            // У MS SQL Server / PostgreSQL створилася б таблиця [userstore].[Employees],
            // але SQLite не має поняття схем (schemas), тому провайдер ігнорує параметр schema.
            modelBuilder.Entity<UserFluent>()
                .ToTable("Employees", schema: "userstore")
                .Property(u => u.Id)
                .HasColumnName("employee_id");
        }
    }
}

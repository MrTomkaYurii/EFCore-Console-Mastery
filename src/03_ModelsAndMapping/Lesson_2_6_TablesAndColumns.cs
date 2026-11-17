using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 2.6: Зіставлення таблиць і стовпців.
/// Демонструє перевизначення імен таблиць ([Table], ToTable)
/// та імен стовпців ([Column], HasColumnName).
/// </summary>
public class Lesson_2_6_TablesAndColumns : ILesson
{
    public string Id => "2.6";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Зіставлення таблиць і стовпців";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.6.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 6;

    public void Run()
    {
        SqliteDbHelper.PrintSection("2.6. Зіставлення таблиць і стовпців");

        // Приклад 1: Анотації даних [Table] та [Column]
        using (var db = new L2_6.AnnotationsContext())
        {
            SqliteDbHelper.RecreateDatabase(db);
            db.Users.Add(new L2_6.UserAnnotated { Name = "Tom" });
            db.SaveChanges();

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
    [Table("People")]
    public class UserAnnotated
    {
        [Column("user_id")]
        public int Id { get; set; }

        [Column("full_name")]
        public string? Name { get; set; }
    }

    public class AnnotationsContext : DbContext
    {
        public DbSet<UserAnnotated> Users => Set<UserAnnotated>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_6_ann"));
        }
    }

    public class UserFluent
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class FluentContext : DbContext
    {
        public DbSet<UserFluent> Users => Set<UserFluent>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_6_fl"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Параметр schema корисний для SQL Server / PostgreSQL; у SQLite він ігнорується
            modelBuilder.Entity<UserFluent>()
                .ToTable("Employees", schema: "userstore")
                .Property(u => u.Id)
                .HasColumnName("employee_id");
        }
    }
}

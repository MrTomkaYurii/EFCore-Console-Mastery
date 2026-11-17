using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_12;

    /// <summary>
    /// Урок 3.12: Комплексні типи (Complex Types, EF Core 8+).
    /// </summary>
    public class Lesson_3_12_ComplexTypes : ILesson
    {
        public string Id => "3.12";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Комплексні типи (Complex Types)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.12.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 11;

        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Спільне використання одного об'єкта ComplexType різними сутностями");
            using (var db = new AppContextL3_12())
            {
                SqliteDbHelper.RecreateDatabase(db);

                // Один екземпляр комплексного типу може бути призначений кільком різним сутностям
                var csharp = new Language { Name = "C#" };

                var tom = new User { Name = "Tom", Language = csharp };
                var bob = new User { Name = "Bob", Language = tom.Language };
                var microsoft = new Company { Name = "Microsoft", Language = csharp };

                db.Users.AddRange(tom, bob);
                db.Companies.Add(microsoft);
                db.SaveChanges();

                Console.WriteLine("Після додавання (до зміни спільного об'єкта):");
                foreach (var u in db.Users.ToList())
                    Console.WriteLine($"  User: {u.Name} | Language: {u.Language.Name}");
                foreach (var c in db.Companies.ToList())
                    Console.WriteLine($"  Company: {c.Name} | Language: {c.Language.Name}");

                // Оскільки в межах одного контексту tom, bob і microsoft посилаються на той самий об'єкт csharp,
                // зміна властивості csharp.Name вплине на всі три сутності перед SaveChanges()
                csharp.Name = "C# (.NET)";
                db.SaveChanges();
            }

            SqliteDbHelper.PrintSection("2. Читання з нового контексту після SaveChanges()");
            using (var db = new AppContextL3_12())
            {
                foreach (var u in db.Users.ToList())
                    Console.WriteLine($"  User: {u.Name} | Language: {u.Language.Name}");
                foreach (var c in db.Companies.ToList())
                    Console.WriteLine($"  Company: {c.Name} | Language: {c.Language.Name}");
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_12
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required Language Language { get; set; }
    }

    public class Company
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required Language Language { get; set; }
    }

    /// <summary>
    /// Комплексний тип (EF Core 8+): не має первинного ключа, не є окремою таблицею,
    /// але на відміну від [Owned] один екземпляр можна присвоїти кільком сутностям.
    /// </summary>
    [ComplexType]
    public class Language
    {
        public required string Name { get; set; }
    }

    public class AppContextL3_12 : DbContext
    {
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_12"));
        }
    }
}

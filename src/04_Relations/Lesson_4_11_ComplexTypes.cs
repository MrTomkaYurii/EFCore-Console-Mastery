using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_12;

    /// <summary>
    /// Урок 4.11: Комплексні типи (Complex Types, EF Core 8+).
    /// </summary>
    public class Lesson_4_11_ComplexTypes : ILesson
    {
        public string Id => "4.11";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Комплексні типи (Complex Types)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.12.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 11;

        /// <summary>
        /// Демонструє роботу з комплексними типами (Complex Types, EF Core 8+) — об'єктами-значеннями (Value Objects),
        /// які вбудовуються в таблицю власника і, на відміну від <c>[Owned]</c>, можуть спільно використовуватися кількома сутностями.
        /// </summary>
        public void Run()
        {
            SqlServerDbHelper.PrintSection("1. Спільне використання одного об'єкта ComplexType різними сутностями");
            using (var db = new AppContextL3_12())
            {
                SqlServerDbHelper.RecreateSchema(db, "l4_11");

                // Один екземпляр комплексного типу може бути призначений кільком різним сутностям (навіть різних типів: User та Company).
                // Для [Owned] таке спільне посилання викликало б конфлікт відстеження, бо Owned-тип має приховану ідентичність власника.
                // У таблицях Users та Companies у SQLite створюється стовпець Language_Name (TEXT NOT NULL).
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

                // Оскільки в межах одного контексту tom, bob і microsoft посилаються на той самий екземпляр csharp у пам'яті,
                // зміна csharp.Name при SaveChanges() згенерує UPDATE і для Users (для Tom і Bob), і для Companies (для Microsoft).
                csharp.Name = "C# (.NET)";
                db.SaveChanges();
            }

            SqlServerDbHelper.PrintSection("2. Читання з нового контексту після SaveChanges()");
            using (var db = new AppContextL3_12())
            {
                // При читанні з нового контексту кожна сутність отримує власну копію Language зі значенням зі свого рядка таблиці.
                // Complex Types також підтримують C# struct та record struct і завжди завантажуються без Include().
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
    /// <summary>Користувач, що містить комплексний тип <see cref="Language"/>.</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Обов'язкове ім'я користувача (модифікатор <c>required</c> C# 11+).</summary>
        public required string Name { get; set; }

        /// <summary>Комплексна властивість (стовпець Language_Name у таблиці Users).</summary>
        public required Language Language { get; set; }
    }

    /// <summary>Компанія, що також містить комплексний тип <see cref="Language"/>.</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Обов'язкова назва компанії.</summary>
        public required string Name { get; set; }

        /// <summary>Комплексна властивість (стовпець Language_Name у таблиці Companies).</summary>
        public required Language Language { get; set; }
    }

    /// <summary>
    /// Комплексний тип (EF Core 8+): не має первинного ключа і не створює окремої таблиці в БД,
    /// але на відміну від <c>[Owned]</c> поводиться як Value Object і один екземпляр можна присвоїти кільком сутностям.
    /// </summary>
    [ComplexType]
    public class Language
    {
        /// <summary>Назва мови програмування (вбудовується як стовпець Language_Name у таблицю власника).</summary>
        public required string Name { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 3.12 (SQLite).</summary>
    public class AppContextL3_12 : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("l4_11");
        /// <summary>Компанії.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Користувачі.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }
    }
}

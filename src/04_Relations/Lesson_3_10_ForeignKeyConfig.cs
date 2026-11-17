using System.ComponentModel.DataAnnotations.Schema;
using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_10;

    /// <summary>
    /// Урок 3.10: Налаштування зовнішнього ключа через Data Annotations та Fluent API.
    /// </summary>
    public class Lesson_3_10_ForeignKeyConfig : ILesson
    {
        public string Id => "3.10";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Налаштування зовнішнього ключа через анотації та Fluent API";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.10.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 2;

        /// <summary>
        /// Демонструє налаштування нестандартного імені зовнішнього ключа через атрибут <c>[ForeignKey]</c>
        /// та прив'язку FK до альтернативного ключа через Fluent API <c>HasPrincipalKey</c>.
        /// </summary>
        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Зовнішній ключ через [ForeignKey] та Fluent API");
            using (var db = new AppContextL3_10())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var google = new Company { Name = "Google" };
                var microsoft = new Company { Name = "Microsoft" };
                db.Companies.AddRange(google, microsoft);
                db.SaveChanges();

                // User використовує нестандартну назву зовнішнього ключа CompanyInfoKey (через [ForeignKey] та Fluent API).
                // Без такої конфігурації EF Core не розпізнав би CompanyInfoKey як FK і створив би ще один тіньовий стовпець CompanyId.
                var tom = new User { Name = "Tom", CompanyInfoKey = google.Id };
                var bob = new User { Name = "Bob", Company = microsoft };
                db.Users.AddRange(tom, bob);

                // Employee використовує HasPrincipalKey(c => c.Name) — зовнішній ключ посилається на Name компанії, а не на Id.
                // У SQLite для цього в таблиці Companies автоматично створюється UNIQUE-обмеження (AK_Companies_Name) на стовпці Name.
                var alice = new Employee { Name = "Alice", CompanyName = "Google" };
                var kate = new Employee { Name = "Kate", Company = microsoft };
                db.Employees.AddRange(alice, kate);

                db.SaveChanges();
            }

            using (var db = new AppContextL3_10())
            {
                Console.WriteLine("Користувачі (FK = CompanyInfoKey):");
                foreach (var u in db.Users.Include(u => u.Company).ToList())
                {
                    Console.WriteLine($"- {u.Name}: CompanyInfoKey={u.CompanyInfoKey}, Company={u.Company?.Name}");
                }

                Console.WriteLine("\nПрацівники (FK = CompanyName -> PrincipalKey = Company.Name):");
                foreach (var e in db.Employees.Include(e => e.Company).ToList())
                {
                    Console.WriteLine($"- {e.Name}: CompanyName='{e.CompanyName}', Company.Id={e.Company?.Id}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_10
{
    /// <summary>Компанія, яка виступає головною сутністю для <see cref="User"/> (по <c>Id</c>) та <see cref="Employee"/> (по <c>Name</c>).</summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>
        /// Назва компанії (обов'язкова, не-nullable, оскільки використовується як альтернативний ключ у <c>HasPrincipalKey</c>).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Користувачі компанії (зв'язок через CompanyInfoKey -> Company.Id).</summary>
        public List<User> Users { get; set; } = new();

        /// <summary>Працівники компанії (зв'язок через CompanyName -> Company.Name).</summary>
        public List<Employee> Employees { get; set; } = new();
    }

    /// <summary>Користувач із нестандартним іменем зовнішнього ключа <see cref="CompanyInfoKey"/>.</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Нестандартне ім'я зовнішнього ключа (не відповідає конвенції CompanyId).</summary>
        public int? CompanyInfoKey { get; set; }

        /// <summary>Атрибут <c>[ForeignKey]</c> явно вказує EF Core, що <c>CompanyInfoKey</c> є зовнішнім ключем для навігації <c>Company</c>.</summary>
        [ForeignKey(nameof(CompanyInfoKey))]
        public Company? Company { get; set; }
    }

    /// <summary>Працівник, чий зовнішній ключ посилається на альтернативний ключ <c>Company.Name</c>.</summary>
    public class Employee
    {
        /// <summary>Первинний ключ працівника.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я працівника.</summary>
        public string? Name { get; set; }

        /// <summary>Зовнішній ключ, що посилається на альтернативний ключ (<c>Company.Name</c>), а не на <c>Company.Id</c>.</summary>
        public string? CompanyName { get; set; }

        /// <summary>Навігаційна властивість до компанії.</summary>
        public Company? Company { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 3.10 (SQLite).</summary>
    public class AppContextL3_10 : DbContext
    {
        /// <summary>Користувачі.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Працівники.</summary>
        public DbSet<Employee> Employees { get; set; } = null!;

        /// <summary>Компанії.</summary>
        public DbSet<Company> Companies { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_10"));
        }

        /// <summary>
        /// Налаштовує нестандартні зовнішні та альтернативні ключі через Fluent API.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Явна конфігурація зовнішнього ключа через Fluent API (має пріоритет над атрибутами Data Annotations).
            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyInfoKey);

            // Прив'язка зовнішнього ключа до довільної унікальної властивості (PrincipalKey).
            // EF Core зробить Company.Name альтернативним ключем (UNIQUE CONSTRAINT у SQLite: CONSTRAINT "AK_Companies_Name" UNIQUE ("Name")),
            // бо SQL-стандарт вимагає, щоб ціль FOREIGN KEY була унікальною.
            // Примітка: у SQL Server для строкового PK/AK потрібно також обмежити довжину (HasMaxLength), інакше varchar(max) не можна індексувати;
            // у SQLite таких обмежень на TEXT немає.
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Company)
                .WithMany(c => c.Employees)
                .HasForeignKey(e => e.CompanyName)
                .HasPrincipalKey(c => c.Name);
        }
    }
}

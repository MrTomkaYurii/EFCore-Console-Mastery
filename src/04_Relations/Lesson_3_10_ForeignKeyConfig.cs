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

                // User використовує нестандартну назву зовнішнього ключа CompanyInfoKey (через [ForeignKey])
                var tom = new User { Name = "Tom", CompanyInfoKey = google.Id };
                var bob = new User { Name = "Bob", Company = microsoft };
                db.Users.AddRange(tom, bob);

                // Employee використовує HasPrincipalKey(c => c.Name) — зовнішній ключ посилається на Name компанії
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
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<User> Users { get; set; } = new();
        public List<Employee> Employees { get; set; } = new();
    }

    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        /// <summary>Нестандартне ім'я зовнішнього ключа.</summary>
        public int? CompanyInfoKey { get; set; }

        /// <summary>Атрибут [ForeignKey] вказує, яке саме поле є зовнішнім ключем для цієї навігаційної властивості.</summary>
        [ForeignKey(nameof(CompanyInfoKey))]
        public Company? Company { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        /// <summary>Зовнішній ключ, що посилається на альтернативний ключ (Company.Name), а не на Id.</summary>
        public string? CompanyName { get; set; }
        public Company? Company { get; set; }
    }

    public class AppContextL3_10 : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_10"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Явна конфігурація зовнішнього ключа через Fluent API
            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyInfoKey);

            // Прив'язка зовнішнього ключа до довільної унікальної властивості (PrincipalKey)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Company)
                .WithMany(c => c.Employees)
                .HasForeignKey(e => e.CompanyName)
                .HasPrincipalKey(c => c.Name);
        }
    }
}

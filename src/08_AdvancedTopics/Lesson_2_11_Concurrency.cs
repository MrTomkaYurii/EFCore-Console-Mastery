using System.ComponentModel.DataAnnotations;
using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch08.L2_11;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch08
{
    /// <summary>
    /// Урок 2.11: Паралелізм та оптимістичне блокування (Concurrency).
    /// Демонструє роботу токенів паралелізму (<c>[ConcurrencyCheck]</c> та <c>IsConcurrencyToken()</c>)
    /// у SQLite та обробку винятку <see cref="DbUpdateConcurrencyException"/>.
    /// </summary>
    public class Lesson_2_11_Concurrency : ILesson
    {
        public string Id => "2.11";
        public string Chapter => "08. Додаткові теми";
        public string Title => "Паралелізм та оптимістичне блокування (Concurrency)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/2.11.php";
        public int ChapterOrder => 8;
        public int LessonOrder => 1;

        public void Run()
        {
            var connStr = SqliteDbHelper.GetConnectionString("lesson_2_11_concurrency");

            // 1. Створюємо чисту БД та початкові дані
            using (var setupDb = new ConcurrencyContext(connStr))
            {
                SqliteDbHelper.RecreateDatabase(setupDb);
            }

            // -------------------------------------------------------------------------
            // Сценарій 1: Токен паралелізму через [ConcurrencyCheck] на властивості Name
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("1. Конфлікт паралелізму з [ConcurrencyCheck]");

            using (var db1 = new ConcurrencyContext(connStr))
            using (var db2 = new ConcurrencyContext(connStr))
            {
                // Два незалежні контексти (імітація двох користувачів) завантажують один і той самий запис
                var userForClient1 = db1.Users.First(u => u.Id == 1);
                var userForClient2 = db2.Users.First(u => u.Id == 1);

                Console.WriteLine($"Початковий стан у БД: Name = '{userForClient1.Name}', Version = {userForClient1.Version}");

                // Клієнт 1 змінює ім'я та оновлює токен версії, після чого зберігає зміни
                userForClient1.Name = "Bob";
                userForClient1.Version++;
                db1.SaveChanges();
                Console.WriteLine($"[Клієнт 1] Успішно зберіг зміни: Name = '{userForClient1.Name}', Version = {userForClient1.Version}");

                // Клієнт 2 намагається зберегти свої зміни, маючи застарілі оригінальні значення
                userForClient2.Name = "Alice";
                userForClient2.Version++;

                try
                {
                    db2.SaveChanges();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[Клієнт 2] Перехоплено DbUpdateConcurrencyException: {ex.Message}");
                    Console.ResetColor();

                    // Розв'язання конфлікту: перевіряємо поточні значення в БД (Database Wins або Client Wins)
                    var entry = ex.Entries.Single();
                    var clientValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    if (databaseValues is not null)
                    {
                        Console.WriteLine($"  -> Значення клієнта 2: Name='{clientValues["Name"]}', Version={clientValues["Version"]}");
                        Console.WriteLine($"  -> Актуальне значення в БД: Name='{databaseValues["Name"]}', Version={databaseValues["Version"]}");

                        // Обираємо стратегію "Database Wins" (оновлюємо стан об'єкта даними з БД)
                        entry.OriginalValues.SetValues(databaseValues);
                        entry.CurrentValues.SetValues(databaseValues);
                        Console.WriteLine("  -> Застосовано стратегію 'Database Wins' (перечитано актуальний стан з БД).");
                    }
                }
            }

            // -------------------------------------------------------------------------
            // Сценарій 2: Токен паралелізму через Fluent API IsConcurrencyToken() (Version / Guid)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("2. Токен паралелізму IsConcurrencyToken() з автоматичним оновленням версії");

            // У SQLite немає автоматичного rowversion/Timestamp як у SQL Server,
            // тому використовуємо int/Guid властивість з IsConcurrencyToken(), яку оновлюємо у SaveChanges().
            using (var db1 = new ConcurrencyContext(connStr))
            using (var db2 = new ConcurrencyContext(connStr))
            {
                var doc1 = db1.Documents.First(d => d.Id == 1);
                var doc2 = db2.Documents.First(d => d.Id == 1);

                Console.WriteLine($"Документ у БД: Title='{doc1.Title}', RowVersion={doc1.RowVersion}");

                // Клієнт 1 редагує документ і зберігає (ConcurrencyContext автоматично змінює RowVersion)
                doc1.Title = "Специфікація v2 (від Клієнта 1)";
                db1.SaveChanges();
                Console.WriteLine($"[Клієнт 1] Збережено: Title='{doc1.Title}', новий RowVersion={doc1.RowVersion}");

                // Клієнт 2 редагує той самий документ, але його RowVersion уже застарілий
                doc2.Title = "Специфікація v2 (від Клієнта 2)";
                try
                {
                    db2.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Клієнт 2] Конфлікт версій! Запис уже було змінено іншим користувачем.");
                    Console.ResetColor();

                    // Стратегія "Client Wins" (перезаписуємо БД значенням клієнта 2 після оновлення OriginalValues)
                    var entry = db2.Entry(doc2);
                    var dbValues = entry.GetDatabaseValues();
                    if (dbValues is not null)
                    {
                        // Встановлюємо актуальний RowVersion з БД як OriginalValue, щоб наступний UPDATE пройшов
                        entry.OriginalValues.SetValues(dbValues);
                        // Продовжуємо зберігати локальний Title клієнта 2
                        db2.SaveChanges();
                        Console.WriteLine($"[Клієнт 2] Застосовано 'Client Wins': Title='{doc2.Title}', RowVersion={doc2.RowVersion}");
                    }
                }
            }

            // Перевіряємо фінальний стан у БД
            using (var verifyDb = new ConcurrencyContext(connStr))
            {
                var finalDoc = verifyDb.Documents.First(d => d.Id == 1);
                Console.WriteLine($"Фінальний стан документа в БД: Title='{finalDoc.Title}', RowVersion={finalDoc.RowVersion}");
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch08.L2_11
{
    /// <summary>
    /// Модель користувача з атрибутом <see cref="ConcurrencyCheckAttribute"/> на властивості <see cref="Name"/>.
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        /// <summary>
        /// При оновленні або видаленні EF Core додасть перевірку старого значення Name у WHERE.
        /// </summary>
        [ConcurrencyCheck]
        public string? Name { get; set; }

        /// <summary>
        /// Додатковий числовий маркер версії (налаштований через Fluent API як токен паралелізму).
        /// </summary>
        public int Version { get; set; }
    }

    /// <summary>
    /// Модель документа, що демонструє токен паралелізму <see cref="RowVersion"/> на базі <see cref="Guid"/>
    /// (аналог SQL Server rowversion/Timestamp для SQLite).
    /// </summary>
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Токен паралелізму. У SQL Server це зазвичай <c>byte[]</c> з <c>[Timestamp]</c>,
        /// а в SQLite — <see cref="Guid"/> або <see cref="int"/>, що оновлюється при кожному збереженні.
        /// </summary>
        public Guid RowVersion { get; set; }
    }

    /// <summary>
    /// Контекст даних уроку 2.11 з автоматичним оновленням <see cref="Document.RowVersion"/> у <see cref="SaveChanges"/>.
    /// </summary>
    public class ConcurrencyContext : DbContext
    {
        private readonly string _connectionString;

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;

        public ConcurrencyContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Налаштування токена паралелізму через Fluent API
            modelBuilder.Entity<User>()
                .Property(u => u.Version)
                .IsConcurrencyToken();

            modelBuilder.Entity<Document>()
                .Property(d => d.RowVersion)
                .IsConcurrencyToken();

            // Початкові дані
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "Tom", Version = 1 }
            );

            modelBuilder.Entity<Document>().HasData(
                new Document
                {
                    Id = 1,
                    Title = "Специфікація v1",
                    RowVersion = Guid.Parse("11111111-1111-1111-1111-111111111111")
                }
            );
        }

        public override int SaveChanges()
        {
            // Автоматично генеруємо нову версію для змінених документів перед відправкою UPDATE у SQLite
            foreach (var entry in ChangeTracker.Entries<Document>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Entity.RowVersion = Guid.NewGuid();
                }
            }

            return base.SaveChanges();
        }
    }
}

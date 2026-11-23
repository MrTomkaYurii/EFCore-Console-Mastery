using System.ComponentModel.DataAnnotations;
using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch08.L2_11;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch08
{
    /// <summary>
    /// Урок 8.1: Паралелізм та оптимістичне блокування (Concurrency).
    /// Демонструє роботу токенів паралелізму (<c>[ConcurrencyCheck]</c> та <c>IsConcurrencyToken()</c>)
    /// у SQLite та обробку винятку <see cref="DbUpdateConcurrencyException"/>.
    /// </summary>
    public class Lesson_8_01_Concurrency : ILesson
    {
        public string Id => "8.1";
        public string Chapter => "08. Додаткові теми";
        public string Title => "Паралелізм та оптимістичне блокування (Concurrency)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/2.11.php";
        public int ChapterOrder => 8;
        public int LessonOrder => 1;

        /// <summary>
        /// Запускає демонстрацію оптимістичного блокування (Optimistic Concurrency) у EF Core:
        /// 1) Конфлікт паралелізму з атрибутом <see cref="ConcurrencyCheckAttribute"/> та стратегією "Database Wins".
        /// 2) Токен паралелізму через Fluent API <c>IsConcurrencyToken()</c> з автогенерацією версії
        /// (аналог <c>rowversion</c> у SQL Server для SQLite) та стратегією "Client Wins".
        /// </summary>
        public void Run()
        {
            var connStr = SqliteDbHelper.GetConnectionString("lesson_2_11_concurrency");

            // 1. Створюємо чисту БД та початкові дані (виконується HasData із OnModelCreating)
            using (var setupDb = new ConcurrencyContext(connStr))
            {
                SqliteDbHelper.RecreateDatabase(setupDb);
            }

            // -------------------------------------------------------------------------
            // Сценарій 1: Токен паралелізму через [ConcurrencyCheck] на властивості Name
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("1. Конфлікт паралелізму з [ConcurrencyCheck]");

            // Використовуємо два незалежні DbContext, щоб імітувати двох окремих користувачів/запитів.
            // Кожен контекст має власний ChangeTracker і зберігає власні OriginalValues на момент читання.
            using (var db1 = new ConcurrencyContext(connStr))
            using (var db2 = new ConcurrencyContext(connStr))
            {
                // Обидва контексти виконують SELECT і завантажують той самий рядок із Id = 1
                var userForClient1 = db1.Users.First(u => u.Id == 1);
                var userForClient2 = db2.Users.First(u => u.Id == 1);

                Console.WriteLine($"Початковий стан у БД: Name = '{userForClient1.Name}', Version = {userForClient1.Version}");

                // Клієнт 1 змінює ім'я та інкрементує версію.
                // Оскільки Name має [ConcurrencyCheck], а Version — IsConcurrencyToken(), EF Core згенерує SQL:
                // UPDATE "Users" SET "Name" = @p0, "Version" = @p1
                // WHERE "Id" = @p2 AND "Name" = @p3 AND "Version" = @p4;
                // (де @p3 і @p4 — це OriginalValues: 'Tom' та 1).
                userForClient1.Name = "Bob";
                userForClient1.Version++;
                db1.SaveChanges();
                Console.WriteLine($"[Клієнт 1] Успішно зберіг зміни: Name = '{userForClient1.Name}', Version = {userForClient1.Version}");

                // Клієнт 2 також змінює дані, але його ChangeTracker досі тримає застарілі OriginalValues ('Tom', 1).
                userForClient2.Name = "Alice";
                userForClient2.Version++;

                try
                {
                    // Під час SaveChanges() умова WHERE "Name" = 'Tom' AND "Version" = 1 не знайде жодного рядка,
                    // SQLite поверне 0 змінених рядків (changes() = 0), і EF Core викине DbUpdateConcurrencyException.
                    db2.SaveChanges();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[Клієнт 2] Перехоплено DbUpdateConcurrencyException: {ex.Message}");
                    Console.ResetColor();

                    // Розв'язання конфлікту: ex.Entries містить сутності, які не вдалося зберегти.
                    var entry = ex.Entries.Single();
                    var clientValues = entry.CurrentValues;
                    // GetDatabaseValues() виконує додатковий SELECT до БД, щоб отримати актуальні значення,
                    // не перезаписуючи при цьому CurrentValues у пам'яті.
                    var databaseValues = entry.GetDatabaseValues();

                    if (databaseValues is not null)
                    {
                        Console.WriteLine($"  -> Значення клієнта 2: Name='{clientValues["Name"]}', Version={clientValues["Version"]}");
                        Console.WriteLine($"  -> Актуальне значення в БД: Name='{databaseValues["Name"]}', Version={databaseValues["Version"]}");

                        // Стратегія "Database Wins": відмовляємося від локальних змін клієнта 2
                        // та оновлюємо і OriginalValues, і CurrentValues актуальними даними з БД.
                        entry.OriginalValues.SetValues(databaseValues);
                        entry.CurrentValues.SetValues(databaseValues);
                        Console.WriteLine("  -> Застосовано стратегію 'Database Wins' (перечитано актуальний стан з БД).");
                    }
                    // Примітка: якщо databaseValues == null, це означає, що запис уже видалено іншим користувачем.
                }
            }

            // -------------------------------------------------------------------------
            // Сценарій 2: Токен паралелізму через Fluent API IsConcurrencyToken() (Version / Guid)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("2. Токен паралелізму IsConcurrencyToken() з автоматичним оновленням версії");

            // Важлива відмінність від MS SQL Server:
            // У SQL Server є тип стовпця rowversion (атрибут [Timestamp] на byte[]), який СУБД оновлює сама.
            // У SQLite немає автоматичного rowversion, тому ми використовуємо Guid + IsConcurrencyToken()
            // і генеруємо нове значення вручну в перевизначеному SaveChanges().
            using (var db1 = new ConcurrencyContext(connStr))
            using (var db2 = new ConcurrencyContext(connStr))
            {
                var doc1 = db1.Documents.First(d => d.Id == 1);
                var doc2 = db2.Documents.First(d => d.Id == 1);

                Console.WriteLine($"Документ у БД: Title='{doc1.Title}', RowVersion={doc1.RowVersion}");

                // Клієнт 1 редагує документ і зберігає (ConcurrencyContext.SaveChanges автоматично змінює RowVersion).
                // У WHERE перевіряється старий RowVersion, а в SET записується новий Guid.
                doc1.Title = "Специфікація v2 (від Клієнта 1)";
                db1.SaveChanges();
                Console.WriteLine($"[Клієнт 1] Збережено: Title='{doc1.Title}', новий RowVersion={doc1.RowVersion}");

                // Клієнт 2 редагує той самий документ, але його OriginalValues["RowVersion"] уже застарілий.
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

                    // Стратегія "Client Wins": наполягаємо на збереженні змін клієнта 2.
                    var entry = db2.Entry(doc2);
                    var dbValues = entry.GetDatabaseValues();
                    if (dbValues is not null)
                    {
                        // Оновлюємо ЛИШЕ OriginalValues актуальними значеннями з БД (зокрема новим RowVersion),
                        // щоб умова WHERE у наступному UPDATE збіглася з поточним станом у БД.
                        // CurrentValues (Title клієнта 2) залишаємо незмінними!
                        entry.OriginalValues.SetValues(dbValues);
                        db2.SaveChanges();
                        Console.WriteLine($"[Клієнт 2] Застосовано 'Client Wins': Title='{doc2.Title}', RowVersion={doc2.RowVersion}");
                    }
                }
            }

            // Перевіряємо фінальний стан у БД новим контекстом, щоб переконатися в збереженні змін
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
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача. Завдяки <see cref="ConcurrencyCheckAttribute"/> при UPDATE або DELETE
        /// EF Core додасть перевірку старого значення Name у секцію WHERE (AND "Name" = @oldName).
        /// </summary>
        [ConcurrencyCheck]
        public string? Name { get; set; }

        /// <summary>
        /// Додатковий числовий маркер версії (налаштований через Fluent API <c>IsConcurrencyToken()</c>).
        /// На відміну від автоматичного <c>rowversion</c>, розробник повинен інкрементувати його вручну.
        /// </summary>
        public int Version { get; set; }
    }

    /// <summary>
    /// Модель документа, що демонструє токен паралелізму <see cref="RowVersion"/> на базі <see cref="Guid"/>
    /// (аналог SQL Server <c>rowversion</c>/<c>[Timestamp]</c> для SQLite).
    /// </summary>
    public class Document
    {
        /// <summary>Первинний ключ документа.</summary>
        public int Id { get; set; }

        /// <summary>Назва або заголовок документа.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Токен паралелізму. У SQL Server це зазвичай <c>byte[]</c> з <c>[Timestamp]</c> (оновлюється СУБД),
        /// а в SQLite — <see cref="Guid"/> або <see cref="int"/>, що оновлюється у <see cref="ConcurrencyContext.SaveChanges"/>.
        /// </summary>
        public Guid RowVersion { get; set; }
    }

    /// <summary>
    /// Контекст даних уроку 2.11 з налаштованими токенами паралелізму
    /// та автоматичним оновленням <see cref="Document.RowVersion"/> у <see cref="SaveChanges"/>.
    /// </summary>
    public class ConcurrencyContext : DbContext
    {
        private readonly string _connectionString;

        /// <summary>Таблиця користувачів для демонстрації <c>[ConcurrencyCheck]</c>.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Таблиця документів для демонстрації <c>IsConcurrencyToken()</c> з <see cref="Guid"/>.</summary>
        public DbSet<Document> Documents { get; set; } = null!;

        /// <summary>
        /// Ініціалізує контекст із рядком підключення до SQLite.
        /// </summary>
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
            // Налаштування токена паралелізму через Fluent API (альтернатива атрибуту [ConcurrencyCheck])
            modelBuilder.Entity<User>()
                .Property(u => u.Version)
                .IsConcurrencyToken();

            modelBuilder.Entity<Document>()
                .Property(d => d.RowVersion)
                .IsConcurrencyToken();

            // Початкові дані (Seeding): у HasData значення мають бути детермінованими (фіксований Guid, не Guid.NewGuid()),
            // інакше EF Core вважатиме модель зміненою при кожному запуску чи створенні міграцій.
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

        /// <summary>
        /// Автоматично генерує новий <see cref="Document.RowVersion"/> перед збереженням,
        /// імітуючи поведінку <c>rowversion</c> у SQL Server.
        /// </summary>
        public override int SaveChanges()
        {
            // ChangeTracker зберігає старе значення в OriginalValues (для WHERE),
            // а запис нового Guid у властивість змінює CurrentValues (для SET).
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

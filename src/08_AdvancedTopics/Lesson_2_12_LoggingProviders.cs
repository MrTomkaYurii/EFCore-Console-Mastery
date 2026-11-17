using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch08.L2_12;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace EFCoreConsoleMastery.Topics.Ch08
{
    /// <summary>
    /// Урок 2.12: Провайдери логування (<see cref="ILoggerFactory"/>, власний <see cref="ILoggerProvider"/>, Console Logger).
    /// </summary>
    public class Lesson_2_12_LoggingProviders : ILesson
    {
        public string Id => "2.12";
        public string Chapter => "08. Додаткові теми";
        public string Title => "Провайдери логування (ILoggerFactory, Console Logger)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/2.12.php";
        public int ChapterOrder => 8;
        public int LessonOrder => 2;

        /// <summary>
        /// Запускає демонстрацію трьох підходів до логування в EF Core через <see cref="ILoggerFactory"/>:
        /// 1) Локальне підключення власного <see cref="ILoggerProvider"/> через <c>GetService&lt;ILoggerFactory&gt;()</c>.
        /// 2) Зовнішній <see cref="ILoggerFactory"/> із фільтрацією категорії <see cref="DbLoggerCategory.Database.Command"/>.
        /// 3) Стандартний консольний провайдер <c>AddConsole()</c>.
        /// </summary>
        public void Run()
        {
            var connStr = SqliteDbHelper.GetConnectionString("lesson_2_12_logging");
            var logFilePath = Path.Combine(
                Path.GetDirectoryName(SqliteDbHelper.GetDbPath("lesson_2_12_logging"))!,
                "lesson_2_12_efcore.log");

            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            // -------------------------------------------------------------------------
            // 1. Локальне підключення власного ILoggerProvider через GetService<ILoggerFactory>()
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("1. Власний ILoggerProvider (локальне підключення до контексту)");

            using (var db = new LoggingContext(connStr))
            {
                SqliteDbHelper.RecreateDatabase(db);

                // GetService<ILoggerFactory>() отримує ILoggerFactory із внутрішнього DI-контейнера EF Core.
                // Додавання провайдера тут не потребує переконфігурації OnConfiguring, але зачіпає
                // внутрішній ServiceProvider (який EF Core кешує між контекстами з однаковою конфігурацією).
                var loggerFactory = db.GetService<ILoggerFactory>();
                loggerFactory.AddProvider(new CustomFileAndConsoleLoggerProvider(logFilePath, filterSqlOnly: false));

                db.Users.AddRange(
                    new User { Name = "Tom", Age = 33 },
                    new User { Name = "Alice", Age = 26 }
                );
                // Логер зафіксує події DbUpdate, транзакцію SQLite та INSERT-команди з часом виконання (ms).
                // За замовчуванням значення параметрів (@p0, @p1) приховані ('?') без EnableSensitiveDataLogging().
                db.SaveChanges();

                var users = db.Users.ToList();
                Console.WriteLine($"Зчитано користувачів: {users.Count}");
            }

            // -------------------------------------------------------------------------
            // 2. Глобальне налаштування ILoggerFactory з фільтрацією за категорією та LogLevel
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("2. Глобальний ILoggerFactory + фільтр DbLoggerCategory.Database.Command");

            // ВАЖЛИВО: У реальних застосунках ILoggerFactory потрібно оголошувати як static readonly
            // або отримувати з DI-контейнера. Якщо створювати новий LoggerFactory на кожен екземпляр DbContext,
            // EF Core щоразу будуватиме новий внутрішній IServiceProvider, що призведе до витоку пам'яті!
            using (var sqlOnlyFactory = LoggerFactory.Create(builder =>
            {
                builder
                    // Фільтруємо лише виконані SQL-команди (DbLoggerCategory.Database.Command) рівня Information.
                    // Інші категорії (ChangeTracking, Query, Infrastructure) будуть відкинуті ще до виклику Log().
                    .AddFilter((category, level) =>
                        category == DbLoggerCategory.Database.Command.Name
                        && level == LogLevel.Information)
                    .AddProvider(new CustomFileAndConsoleLoggerProvider(logFilePath, filterSqlOnly: true));
            }))
            {
                using var db = new LoggingContext(connStr, sqlOnlyFactory);
                // У SQLite LINQ-запит транслюється в SELECT "u"."Id", "u"."Age", "u"."Name" FROM "Users" AS "u" WHERE "u"."Age" >= 25
                var adults = db.Users.Where(u => u.Age >= 25).ToList();
                foreach (var u in adults)
                {
                    Console.WriteLine($"  -> {u.Id}. {u.Name} ({u.Age} р.)");
                }
            }

            // -------------------------------------------------------------------------
            // 3. Вбудований Console Logger (Microsoft.Extensions.Logging.Console)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("3. Вбудований Console Logger (builder.AddConsole())");

            // AddConsole() використовує фонову чергу для запису в консоль, а блок using
            // гарантує скидання (flush) буфера при завершенні роботи фабрики.
            using (var consoleFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Information)
                    .AddConsole();
            }))
            {
                using var db = new LoggingContext(connStr, consoleFactory);
                // Count() згенерує SQL: SELECT COUNT(*) FROM "Users"
                var count = db.Users.Count();
                Console.WriteLine($"Загальна кількість записів у таблиці Users: {count}");
            }

            Console.WriteLine($"\nУсі логи власного провайдера також збережено у файл:\n  {logFilePath}");
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch08.L2_12
{
    /// <summary>
    /// Проста модель користувача для генерації SQL-запитів та демонстрації логування.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача (може бути null).</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст даних уроку 2.12, що підтримує опційне підключення зовнішньої <see cref="ILoggerFactory"/>.
    /// </summary>
    public class LoggingContext : DbContext
    {
        private readonly string _connectionString;
        private readonly ILoggerFactory? _loggerFactory;

        /// <summary>Таблиця користувачів у SQLite.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Ініціалізує контекст із рядком підключення та необов'язковою фабрикою логерів.
        /// </summary>
        public LoggingContext(string connectionString, ILoggerFactory? loggerFactory = null)
        {
            _connectionString = connectionString;
            _loggerFactory = loggerFactory;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);

            if (_loggerFactory is not null)
            {
                // Підключаємо зовнішню фабрику логерів замість стандартної внутрішньої фабрики EF Core.
                // Альтернатива для швидкого налагодження без ILoggerFactory: optionsBuilder.LogTo(Console.WriteLine).
                optionsBuilder.UseLoggerFactory(_loggerFactory);
            }
        }
    }

    /// <summary>
    /// Власний провайдер логування, що реалізує <see cref="ILoggerProvider"/>.
    /// Записує повідомлення у текстовий файл та виводить їх у консоль.
    /// </summary>
    public sealed class CustomFileAndConsoleLoggerProvider : ILoggerProvider
    {
        private readonly string _logFilePath;
        private readonly bool _filterSqlOnly;

        /// <summary>
        /// Створює провайдер із вказаним шляхом до файлу логів та режимом фільтрації виводу в консоль.
        /// </summary>
        public CustomFileAndConsoleLoggerProvider(string logFilePath, bool filterSqlOnly)
        {
            _logFilePath = logFilePath;
            _filterSqlOnly = filterSqlOnly;
        }

        /// <summary>
        /// Викликається інфраструктурою <see cref="ILoggerFactory"/> для кожної категорії логів EF Core
        /// (наприклад, <c>Microsoft.EntityFrameworkCore.Database.Command</c>).
        /// </summary>
        public ILogger CreateLogger(string categoryName) =>
            new CustomLogger(categoryName, _logFilePath, _filterSqlOnly);

        /// <summary>Звільняє ресурси провайдера (тут додаткових некерованих ресурсів немає).</summary>
        public void Dispose() { }

        private sealed class CustomLogger : ILogger, IDisposable
        {
            private readonly string _categoryName;
            private readonly string _logFilePath;
            private readonly bool _filterSqlOnly;

            public CustomLogger(string categoryName, string logFilePath, bool filterSqlOnly)
            {
                _categoryName = categoryName;
                _logFilePath = logFilePath;
                _filterSqlOnly = filterSqlOnly;
            }

            // BeginScope дозволяє групувати пов'язані логи; повертаємо this як заглушку IDisposable
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => this;

            public void Dispose() { }

            // Відсікаємо Trace та Debug повідомлення на рівні самого логера для економії ресурсів
            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                // formatter перетворює структурований об'єкт state та виняток у готовий текстовий рядок
                var message = formatter(state, exception);
                var tag = _filterSqlOnly ? "SQL-LOG" : "CUSTOM-LOG";
                var formatted = $"[{tag} | {logLevel} | {_categoryName}] {message}{Environment.NewLine}";

                // У навчальному прикладі дописуємо синхронно у файл; у production краще використовувати буферизований запис
                File.AppendAllText(_logFilePath, formatted);

                // Щоб не засмічувати консоль у режимі без фільтра, виводимо лише SQL-команди або короткі повідомлення
                if (!_filterSqlOnly || _categoryName == DbLoggerCategory.Database.Command.Name)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"[{tag}] {message}");
                    Console.ResetColor();
                }
            }
        }
    }
}

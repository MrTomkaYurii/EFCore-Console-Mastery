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

                // Отримуємо вбудований ILoggerFactory конкретного екземпляра DbContext
                // та реєструємо власний провайдер, що пише у файл і в консоль
                var loggerFactory = db.GetService<ILoggerFactory>();
                loggerFactory.AddProvider(new CustomFileAndConsoleLoggerProvider(logFilePath, filterSqlOnly: false));

                db.Users.AddRange(
                    new User { Name = "Tom", Age = 33 },
                    new User { Name = "Alice", Age = 26 }
                );
                db.SaveChanges();

                var users = db.Users.ToList();
                Console.WriteLine($"Зчитано користувачів: {users.Count}");
            }

            // -------------------------------------------------------------------------
            // 2. Глобальне налаштування ILoggerFactory з фільтрацією за категорією та LogLevel
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("2. Глобальний ILoggerFactory + фільтр DbLoggerCategory.Database.Command");

            // У реальних застосунках ILoggerFactory зазвичай оголошують як static readonly у DbContext,
            // щоб EF Core не створював новий екземпляр провайдера на кожен запит.
            using (var sqlOnlyFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter((category, level) =>
                        category == DbLoggerCategory.Database.Command.Name
                        && level == LogLevel.Information)
                    .AddProvider(new CustomFileAndConsoleLoggerProvider(logFilePath, filterSqlOnly: true));
            }))
            {
                using var db = new LoggingContext(connStr, sqlOnlyFactory);
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

            using (var consoleFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Information)
                    .AddConsole();
            }))
            {
                using var db = new LoggingContext(connStr, consoleFactory);
                var count = db.Users.Count();
                Console.WriteLine($"Загальна кількість записів у таблиці Users: {count}");
            }

            Console.WriteLine($"\nУсі логи власного провайдера також збережено у файл:\n  {logFilePath}");
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch08.L2_12
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class LoggingContext : DbContext
    {
        private readonly string _connectionString;
        private readonly ILoggerFactory? _loggerFactory;

        public DbSet<User> Users { get; set; } = null!;

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
                // Підключаємо зовнішню фабрику логерів для цього контексту
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

        public CustomFileAndConsoleLoggerProvider(string logFilePath, bool filterSqlOnly)
        {
            _logFilePath = logFilePath;
            _filterSqlOnly = filterSqlOnly;
        }

        public ILogger CreateLogger(string categoryName) =>
            new CustomLogger(categoryName, _logFilePath, _filterSqlOnly);

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

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => this;

            public void Dispose() { }

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                var tag = _filterSqlOnly ? "SQL-LOG" : "CUSTOM-LOG";
                var formatted = $"[{tag} | {logLevel} | {_categoryName}] {message}{Environment.NewLine}";

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

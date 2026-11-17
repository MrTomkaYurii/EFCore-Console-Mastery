using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch08.L8_3;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFCoreConsoleMastery.Topics.Ch08
{
    /// <summary>
    /// Урок 8.3: Зберігання історії змін (Temporal Tables у SQL Server / Audit-таблиця + SaveChangesInterceptor у SQLite).
    /// </summary>
    public class Lesson_8_3_ChangeHistory : ILesson
    {
        public string Id => "8.3";
        public string Chapter => "08. Додаткові теми";
        public string Title => "Зберігання історії змін (Temporal Tables / SQLite Audit Trigger)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/8.3.php";
        public int ChapterOrder => 8;
        public int LessonOrder => 5;

        public void Run()
        {
            var connStr = SqliteDbHelper.GetConnectionString("lesson_8_3_change_history");

            // 1. Створюємо чисту БД
            using (var setupDb = new AuditHistoryContext(connStr))
            {
                SqliteDbHelper.RecreateDatabase(setupDb);
            }

            // -------------------------------------------------------------------------
            // 2. Виконуємо серію змін (INSERT -> UPDATE -> UPDATE -> DELETE)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("1. Внесення змін до таблиці Users (аудит через SaveChangesInterceptor)");

            int createdUserId;
            using (var db = new AuditHistoryContext(connStr))
            {
                var tom = new User { Name = "Tom" };
                db.Users.Add(tom);
                db.SaveChanges();
                createdUserId = tom.Id;
                Console.WriteLine($"Додано користувача: Id={tom.Id}, Name='{tom.Name}'");
            }

            using (var db = new AuditHistoryContext(connStr))
            {
                var user = db.Users.First(u => u.Id == createdUserId);
                user.Name = "Bob";
                db.SaveChanges();
                Console.WriteLine($"Оновлено користувача: Id={user.Id}, Name='{user.Name}'");

                user.Name = "Sam";
                db.SaveChanges();
                Console.WriteLine($"Оновлено користувача: Id={user.Id}, Name='{user.Name}'");
            }

            using (var db = new AuditHistoryContext(connStr))
            {
                var user = db.Users.First(u => u.Id == createdUserId);
                db.Users.Remove(user);
                db.SaveChanges();
                Console.WriteLine($"Видалено користувача: Id={user.Id}, Name='{user.Name}'");
            }

            // -------------------------------------------------------------------------
            // 3. Перегляд повної історії змін (аналог TemporalAll() у SQL Server)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("2. Перегляд історії змін (аналог TemporalAll())");

            using (var db = new AuditHistoryContext(connStr))
            {
                var history = db.UserHistories
                    .Where(h => h.UserId == createdUserId)
                    .OrderBy(h => h.Id)
                    .ToList();

                foreach (var h in history)
                {
                    Console.WriteLine(
                        $"  [{h.ChangedAtUtc:HH:mm:ss.fff}] {h.Operation,-6} | UserId={h.UserId} | '{h.OldName ?? "NULL"}' -> '{h.NewName ?? "NULL"}'");
                }
            }

            // -------------------------------------------------------------------------
            // 4. Відновлення видаленого запису з історії (аналог TemporalAsOf() + відновлення)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("3. Відновлення стану з історії змін");

            using (var db = new AuditHistoryContext(connStr))
            {
                // Знаходимо останній стан перед видаленням (операція DELETE містить OldName)
                var deletedSnapshot = db.UserHistories
                    .Where(h => h.UserId == createdUserId && h.Operation == "DELETE")
                    .OrderByDescending(h => h.Id)
                    .FirstOrDefault();

                if (deletedSnapshot is not null)
                {
                    // У SQLite, на відміну від SQL Server, для INTEGER PRIMARY KEY
                    // не потрібен SET IDENTITY_INSERT ON — можна просто вставити запис із тим самим Id
                    var restoredUser = new User
                    {
                        Id = deletedSnapshot.UserId,
                        Name = deletedSnapshot.OldName
                    };

                    db.Users.Add(restoredUser);
                    db.SaveChanges();

                    Console.WriteLine($"Відновлено користувача: Id={restoredUser.Id}, Name='{restoredUser.Name}'");
                }

                var currentUsers = db.Users.ToList();
                Console.WriteLine($"Поточний вміст таблиці Users: {string.Join(", ", currentUsers.Select(u => $"{u.Id}:{u.Name}"))}");
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch08.L8_3
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>
    /// Таблиця аудиту (історії змін) для сутності <see cref="User"/> у SQLite.
    /// </summary>
    public class UserHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string? OldName { get; set; }
        public string? NewName { get; set; }
        public DateTime ChangedAtUtc { get; set; }
    }

    /// <summary>
    /// Інтерсептор <see cref="SaveChangesInterceptor"/>, що перехоплює зміни <see cref="User"/>
    /// та автоматично записує історію в таблицю <see cref="UserHistory"/>.
    /// </summary>
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private bool _isSavingAudit;
        private readonly List<(EntityEntry<User> Entry, UserHistory History)> _pendingLogs = new();

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (_isSavingAudit || eventData.Context is null)
            {
                return base.SavingChanges(eventData, result);
            }

            _pendingLogs.Clear();
            var now = DateTime.UtcNow;

            foreach (var entry in eventData.Context.ChangeTracker.Entries<User>())
            {
                if (entry.State == EntityState.Added)
                {
                    _pendingLogs.Add((entry, new UserHistory
                    {
                        Operation = "INSERT",
                        OldName = null,
                        NewName = entry.Entity.Name,
                        ChangedAtUtc = now
                    }));
                }
                else if (entry.State == EntityState.Modified)
                {
                    _pendingLogs.Add((entry, new UserHistory
                    {
                        UserId = entry.Entity.Id,
                        Operation = "UPDATE",
                        OldName = entry.OriginalValues.GetValue<string?>(nameof(User.Name)),
                        NewName = entry.CurrentValues.GetValue<string?>(nameof(User.Name)),
                        ChangedAtUtc = now
                    }));
                }
                else if (entry.State == EntityState.Deleted)
                {
                    _pendingLogs.Add((entry, new UserHistory
                    {
                        UserId = entry.Entity.Id,
                        Operation = "DELETE",
                        OldName = entry.OriginalValues.GetValue<string?>(nameof(User.Name)),
                        NewName = null,
                        ChangedAtUtc = now
                    }));
                }
            }

            return base.SavingChanges(eventData, result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (_isSavingAudit || eventData.Context is null || _pendingLogs.Count == 0)
            {
                return base.SavedChanges(eventData, result);
            }

            try
            {
                _isSavingAudit = true;

                // Після виконання основного SaveChanges уже згенеровані реальні Id для INSERT
                foreach (var (entry, history) in _pendingLogs)
                {
                    if (history.UserId == 0)
                    {
                        history.UserId = entry.Entity.Id;
                    }

                    eventData.Context.Set<UserHistory>().Add(history);
                }

                _pendingLogs.Clear();
                eventData.Context.SaveChanges();
            }
            finally
            {
                _isSavingAudit = false;
            }

            return base.SavedChanges(eventData, result);
        }
    }

    public class AuditHistoryContext : DbContext
    {
        private readonly string _connectionString;
        private readonly AuditSaveChangesInterceptor _auditInterceptor = new();

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserHistory> UserHistories { get; set; } = null!;

        public AuditHistoryContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(_connectionString)
                .AddInterceptors(_auditInterceptor);
        }
    }
}

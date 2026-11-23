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
    public class Lesson_8_03_ChangeHistory : ILesson
    {
        public string Id => "8.3";
        public string Chapter => "08. Додаткові теми";
        public string Title => "Зберігання історії змін (Temporal Tables / SQLite Audit Trigger)";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/8.3.php";
        public int ChapterOrder => 8;
        public int LessonOrder => 5;

        /// <summary>
        /// Запускає демонстрацію збереження історії змін (аудиту) у SQLite:
        /// 1) Виконання серії операцій INSERT / UPDATE / DELETE із перехопленням у <see cref="AuditSaveChangesInterceptor"/>.
        /// 2) Читання повної історії змін із таблиці <c>UserHistories</c> (аналог <c>TemporalAll()</c> у SQL Server).
        /// 3) Відновлення видаленого запису зі збереженням початкового <c>Id</c> (аналог <c>TemporalAsOf()</c> + відновлення).
        /// </summary>
        public void Run()
        {
            var connStr = SqliteDbHelper.GetConnectionString("lesson_8_3_change_history");

            // 1. Створюємо чисту БД (таблиці Users та UserHistories)
            using (var setupDb = new AuditHistoryContext(connStr))
            {
                SqliteDbHelper.RecreateDatabase(setupDb);
            }

            // -------------------------------------------------------------------------
            // 2. Виконуємо серію змін (INSERT -> UPDATE -> UPDATE -> DELETE)
            // -------------------------------------------------------------------------
            SqliteDbHelper.PrintSection("1. Внесення змін до таблиці Users (аудит через SaveChangesInterceptor)");

            // Відмінність від MS SQL Server:
            // У SQL Server є вбудовані Temporal Tables (ToTable(t => t.IsTemporal())), де сама СУБД
            // автоматично переносить попередні версії рядків у таблицю історії.
            // У SQLite темпоральних таблиць немає, тому використовуємо SaveChangesInterceptor + таблицю аудиту
            // (альтернатива — SQLite-тригери AFTER INSERT/UPDATE/DELETE, які працюють навіть для ExecuteUpdate/ExecuteDelete).
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
                // У SQL Server це робиться через db.Users.TemporalAll().Where(u => u.Id == createdUserId)
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
                    // Важлива відмінність SQLite від MS SQL Server:
                    // У SQL Server вставка явного Id у стовпець IDENTITY вимагає SET IDENTITY_INSERT Users ON.
                    // У SQLite стовпець INTEGER PRIMARY KEY дозволяє напряму вставляти заданий Id,
                    // якщо такого Id зараз немає в таблиці.
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
    /// <summary>
    /// Основна модель користувача, зміни якої автоматично протоколюються в <see cref="UserHistory"/>.
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Таблиця аудиту (історії змін) для сутності <see cref="User"/> у SQLite.
    /// Зберігає кожну операцію INSERT, UPDATE та DELETE із фіксацією старого та нового значень.
    /// </summary>
    public class UserHistory
    {
        /// <summary>Первинний ключ запису історії.</summary>
        public int Id { get; set; }

        /// <summary>Ідентифікатор користувача (<see cref="User.Id"/>), якого стосується зміна.</summary>
        public int UserId { get; set; }

        /// <summary>Тип SQL-операції: <c>INSERT</c>, <c>UPDATE</c> або <c>DELETE</c>.</summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>Попереднє значення <see cref="User.Name"/> (null для операції <c>INSERT</c>).</summary>
        public string? OldName { get; set; }

        /// <summary>Нове значення <see cref="User.Name"/> (null для операції <c>DELETE</c>).</summary>
        public string? NewName { get; set; }

        /// <summary>Час фіксації зміни в UTC.</summary>
        public DateTime ChangedAtUtc { get; set; }
    }

    /// <summary>
    /// Інтерсептор <see cref="SaveChangesInterceptor"/>, що перехоплює зміни <see cref="User"/>
    /// у два етапи (<see cref="SavingChanges"/> + <see cref="SavedChanges"/>)
    /// та автоматично записує історію в таблицю <see cref="UserHistory"/>.
    /// </summary>
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        // Прапорець захисту від нескінченної рекурсії при виклику SaveChanges() всередині SavedChanges()
        private bool _isSavingAudit;
        private readonly List<(EntityEntry<User> Entry, UserHistory History)> _pendingLogs = new();

        /// <summary>
        /// Етап 1 (ДО виконання SQL у БД): зчитує стани сутностей із <c>ChangeTracker</c>,
        /// поки доступні <c>EntityState</c>, <c>OriginalValues</c> та <c>CurrentValues</c>.
        /// </summary>
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
                    // Для нових записів UserId ще може дорівнювати 0 (якщо Id генерує SQLite),
                    // тому фактичний Id буде проставлено в SavedChanges уже після INSERT.
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

        /// <summary>
        /// Етап 2 (ПІСЛЯ виконання SQL у БД): доповнює згенеровані БД ключі (<c>Id</c> для INSERT)
        /// та зберігає підготовлені записи аудиту в таблицю <c>UserHistories</c>.
        /// </summary>
        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (_isSavingAudit || eventData.Context is null || _pendingLogs.Count == 0)
            {
                return base.SavedChanges(eventData, result);
            }

            try
            {
                _isSavingAudit = true;

                // Після виконання основного SaveChanges SQLite повернув last_insert_rowid(),
                // і EF Core вже оновив entry.Entity.Id реальним значенням із БД.
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

    /// <summary>
    /// Контекст даних уроку 8.3 із підключеним інтерсептором аудиту <see cref="AuditSaveChangesInterceptor"/>.
    /// </summary>
    public class AuditHistoryContext : DbContext
    {
        private readonly string _connectionString;
        private readonly AuditSaveChangesInterceptor _auditInterceptor = new();

        /// <summary>Основна таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Таблиця історії змін користувачів.</summary>
        public DbSet<UserHistory> UserHistories { get; set; } = null!;

        /// <summary>
        /// Ініціалізує контекст із рядком підключення до SQLite.
        /// </summary>
        public AuditHistoryContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(_connectionString)
                // Реєструємо SaveChangesInterceptor; зверніть увагу, що інтерсептор
                // перехоплює лише SaveChanges/SaveChangesAsync, але не ExecuteUpdate/ExecuteDelete чи ExecuteSqlRaw.
                .AddInterceptors(_auditInterceptor);
        }
    }
}

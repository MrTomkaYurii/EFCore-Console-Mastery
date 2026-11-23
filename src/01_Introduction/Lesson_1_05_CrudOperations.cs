using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 1.5. Основні операції з даними. CRUD.
/// Демонстрація Create, Read, Update (відстежуване та від'єднане), Delete,
/// пакетних методів (AddRange, UpdateRange, RemoveRange) та асинхронного API.
/// </summary>
public class Lesson_1_05_CrudOperations : ILesson
{
    public string Id => "1.5";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Основні операції з даними. CRUD";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.4.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 5;

    /// <summary>
    /// Запускає демонстрацію повного циклу CRUD-операцій (Create, Read, Update, Delete),
    /// відстежуваного та від'єднаного оновлення, пакетних методів і асинхронного API.
    /// </summary>
    public void Run()
    {
        // Оскільки інтерфейс ILesson.Run() синхронний, запускаємо асинхронний метод синхронно
        // (у реальних ASP.NET Core / консольних async Main застосунках слід використовувати await напряму)
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        var connectionString = SqlServerDbHelper.ConnectionString;

        // Початкове перестворення БД в окремому короткоживучому контексті
        using (var setupDb = new L1_4.CrudContext(connectionString))
        {
            SqlServerDbHelper.RecreateSchema(setupDb, "l1_05");
        }

        // 1. CREATE (Додавання: Add та AddRange)
        SqlServerDbHelper.PrintSection("1. Create — Add та AddRange");
        using (var db = new L1_4.CrudContext(connectionString))
        {
            var tom = new L1_4.User { Name = "Tom", Age = 33 };
            var alice = new L1_4.User { Name = "Alice", Age = 26 };
            var kate = new L1_4.User { Name = "Kate", Age = 29 };

            // Add() додає один об'єкт у Change Tracker зі станом Added
            db.Users.Add(tom);
            // AddRange() ефективніше за кілька викликів Add() для пакетного додавання (менше накладних витрат на DetectChanges)
            db.Users.AddRange(alice, kate);
            // EF Core об'єднує всі три INSERT-и в один пакет команд у межах транзакції
            db.SaveChanges();
        }

        // 2. READ (Отримання: ToList)
        SqlServerDbHelper.PrintSection("2. Read — Дані після додавання");
        using (var db = new L1_4.CrudContext(connectionString))
        {
            // У новому контексті Change Tracker спочатку порожній; ToList() завантажує рядки з SQLite
            // і переводить отримані сутності у стан Unchanged
            PrintUsers(db.Users.ToList());
        }

        // 3. UPDATE (Редагування в межах одного контексту — відстеження змін)
        SqlServerDbHelper.PrintSection("3. Update — Редагування відстежуваного об'єкта");
        using (var db = new L1_4.CrudContext(connectionString))
        {
            var user = db.Users.FirstOrDefault();
            if (user is not null)
            {
                user.Name = "Bob";
                user.Age = 44;
                // Викликати db.Users.Update(user) тут не обов'язково — Change Tracker автоматично
                // порівнює поточні значення зі збереженим знімком (snapshot) і генерує UPDATE лише для змінених стовпців
                db.SaveChanges();
            }

            PrintUsers(db.Users.ToList());
        }

        // 4. UPDATE (Від'єднаний сценарій — об'єкт отримано в одному контексті, оновлюється в іншому)
        SqlServerDbHelper.PrintSection("4. Update — Від'єднаний сценарій (Disconnected Update)");
        L1_4.User? detachedUser;
        using (var db1 = new L1_4.CrudContext(connectionString))
        {
            detachedUser = db1.Users.FirstOrDefault(u => u.Name == "Alice");
        } // Після виходу з using db1 знищується, тому detachedUser стає від'єднаним (Detached)

        if (detachedUser is not null)
        {
            detachedUser.Name = "Alice Updated";
            detachedUser.Age = 27;

            using var db2 = new L1_4.CrudContext(connectionString);
            // Оскільки db2 не відстежує detachedUser і не має його початкового знімка,
            // метод Update() примусово позначає ВСІ властивості (окрім ключа Id) як Modified,
            // тому в SQL UPDATE потраплять усі стовпці таблиці (SET "Age" = @p0, "Name" = @p1 WHERE "Id" = @p2)
            db2.Users.Update(detachedUser);
            db2.SaveChanges();

            PrintUsers(db2.Users.ToList());
        }

        // 5. DELETE (Видалення: Remove)
        SqlServerDbHelper.PrintSection("5. Delete — Видалення першого користувача");
        using (var db = new L1_4.CrudContext(connectionString))
        {
            var user = db.Users.FirstOrDefault();
            if (user is not null)
            {
                // Remove() переводить сутність у стан EntityState.Deleted;
                // SaveChanges() виконає DELETE FROM "Users" WHERE "Id" = @p0
                db.Users.Remove(user);
                db.SaveChanges();
            }

            PrintUsers(db.Users.ToList());
        }

        // 6. ASYNC API (AddRangeAsync, ToListAsync, FirstOrDefaultAsync, SaveChangesAsync)
        // Примітка: у SQLite операції вводу-виводу фактично виконуються синхронно на рівні нативної бібліотеки e_sqlite3,
        // але використання Async API є обов'язковою практикою для сумісності з серверними БД (SQL Server, PostgreSQL)
        SqlServerDbHelper.PrintSection("6. Асинхронний CRUD API");
        using (var db = new L1_4.CrudContext(connectionString))
        {
            var sam = new L1_4.User { Name = "Sam", Age = 31 };
            var mike = new L1_4.User { Name = "Mike", Age = 35 };

            await db.Users.AddRangeAsync(sam, mike);
            await db.SaveChangesAsync();

            var found = await db.Users.FirstOrDefaultAsync(u => u.Name == "Sam");
            if (found is not null)
            {
                found.Age = 32;
                await db.SaveChangesAsync();
            }

            var allAsync = await db.Users.ToListAsync();
            PrintUsers(allAsync);
        }
    }

    private static void PrintUsers(IEnumerable<L1_4.User> users)
    {
        foreach (var u in users)
        {
            Console.WriteLine($"  {u.Id}. {u.Name} - {u.Age}");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch01.L1_4
{
    /// <summary>
    /// Сутність користувача для демонстрації CRUD-операцій.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Первинний ключ таблиці Users (INTEGER PRIMARY KEY AUTOINCREMENT у SQLite).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ім'я користувача (TEXT NULL у SQLite).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Вік користувача (INTEGER NOT NULL у SQLite).
        /// </summary>
        public int Age { get; set; }
    }

    /// <summary>
    /// Контекст даних для демонстрації базових операцій створення, читання, оновлення та видалення.
    /// </summary>
    public class CrudContext(string connectionString) : DbContext
    {
        /// <summary>
        /// Набір користувачів, що відповідає таблиці "Users".
        /// </summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>
        /// Налаштовує провайдер SQLite за допомогою переданого рядка підключення.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}

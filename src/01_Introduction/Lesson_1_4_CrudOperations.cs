using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01
{

/// <summary>
/// Урок 1.4. Основні операції з даними. CRUD.
/// Демонстрація Create, Read, Update (відстежуване та від'єднане), Delete,
/// пакетних методів (AddRange, UpdateRange, RemoveRange) та асинхронного API.
/// </summary>
public class Lesson_1_4_CrudOperations : ILesson
{
    public string Id => "1.4";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Основні операції з даними. CRUD";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/1.4.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 5;

    public void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        var connectionString = SqliteDbHelper.GetConnectionString("lesson_1_4");

        // Початкове перестворення БД
        using (var setupDb = new L1_4.CrudContext(connectionString))
        {
            SqliteDbHelper.RecreateDatabase(setupDb);
        }

        // 1. CREATE (Додавання: Add та AddRange)
        SqliteDbHelper.PrintSection("1. Create — Add та AddRange");
        using (var db = new L1_4.CrudContext(connectionString))
        {
            var tom = new L1_4.User { Name = "Tom", Age = 33 };
            var alice = new L1_4.User { Name = "Alice", Age = 26 };
            var kate = new L1_4.User { Name = "Kate", Age = 29 };

            db.Users.Add(tom);
            db.Users.AddRange(alice, kate);
            db.SaveChanges();
        }

        // 2. READ (Отримання: ToList)
        SqliteDbHelper.PrintSection("2. Read — Дані після додавання");
        using (var db = new L1_4.CrudContext(connectionString))
        {
            PrintUsers(db.Users.ToList());
        }

        // 3. UPDATE (Редагування в межах одного контексту — відстеження змін)
        SqliteDbHelper.PrintSection("3. Update — Редагування відстежуваного об'єкта");
        using (var db = new L1_4.CrudContext(connectionString))
        {
            var user = db.Users.FirstOrDefault();
            if (user is not null)
            {
                user.Name = "Bob";
                user.Age = 44;
                // Викликати db.Users.Update(user) тут не обов'язково — Change Tracker сам бачить зміни
                db.SaveChanges();
            }

            PrintUsers(db.Users.ToList());
        }

        // 4. UPDATE (Від'єднаний сценарій — об'єкт отримано в одному контексті, оновлюється в іншому)
        SqliteDbHelper.PrintSection("4. Update — Від'єднаний сценарій (Disconnected Update)");
        L1_4.User? detachedUser;
        using (var db1 = new L1_4.CrudContext(connectionString))
        {
            detachedUser = db1.Users.FirstOrDefault(u => u.Name == "Alice");
        }

        if (detachedUser is not null)
        {
            detachedUser.Name = "Alice Updated";
            detachedUser.Age = 27;

            using var db2 = new L1_4.CrudContext(connectionString);
            // Оскільки db2 не відстежує detachedUser, явно позначаємо його як Modified через Update()
            db2.Users.Update(detachedUser);
            db2.SaveChanges();

            PrintUsers(db2.Users.ToList());
        }

        // 5. DELETE (Видалення: Remove)
        SqliteDbHelper.PrintSection("5. Delete — Видалення першого користувача");
        using (var db = new L1_4.CrudContext(connectionString))
        {
            var user = db.Users.FirstOrDefault();
            if (user is not null)
            {
                db.Users.Remove(user);
                db.SaveChanges();
            }

            PrintUsers(db.Users.ToList());
        }

        // 6. ASYNC API (AddRangeAsync, ToListAsync, FirstOrDefaultAsync, SaveChangesAsync)
        SqliteDbHelper.PrintSection("6. Асинхронний CRUD API");
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
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class CrudContext(string connectionString) : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}

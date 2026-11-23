using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_11;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 6.11: Масове оновлення та видалення. ExecuteUpdate і ExecuteDelete.
/// Демонструє пряме виконання UPDATE та DELETE у SQLite без завантаження сутностей у пам'ять,
/// а також особливість відсутності синхронізації з ChangeTracker.
/// </summary>
public class Lesson_6_11_BulkExecuteUpdateDelete : ILesson
{
    public string Id => "6.11";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Масове оновлення та видалення. ExecuteUpdate і ExecuteDelete";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.11.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 11;

    /// <summary>
    /// Запускає демонстрацію масових операцій <c>ExecuteUpdate</c> та <c>ExecuteDelete</c> (EF Core 7+),
    /// які виконують прямі SQL UPDATE / DELETE без завантаження сутностей і не синхронізують <c>ChangeTracker</c>.
    /// </summary>
    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            db.Users.AddRange(
                new User { Name = "Tom", Age = 36 },
                new User { Name = "Bob", Age = 39 },
                new User { Name = "Alice", Age = 28 },
                new User { Name = "Kate", Age = 25 }
            );
            db.SaveChanges();
        }

        // 1. ExecuteUpdate — масове оновлення без завантаження сутностей
        SqliteDbHelper.PrintSection("1. Масове оновлення через ExecuteUpdate");
        using (var db = new AppDbContext())
        {
            // ExecuteUpdate (з'явився в EF Core 7) виконує UPDATE "Users" SET "Age" = "Age" + 1
            // без попереднього SELECT та без виклику SaveChanges(). Повертає кількість змінених рядків.
            // Це в рази швидше, ніж завантаження всіх сутностей у пам'ять, зміна властивості в циклі й SaveChanges().
            int updatedAll = db.Users.ExecuteUpdate(s => s.SetProperty(u => u.Age, u => u.Age + 1));
            Console.WriteLine($"Оновлено рядків (усім +1 рік): {updatedAll}");

            // Оновлення кількох властивостей одночасно з умовою Where:
            // SetProperty приймає вираз-селектор властивості та вираз нового значення (або константу через _ => "Tomas").
            // Зауваження: на відміну від SaveChanges(), ExecuteUpdate/ExecuteDelete НЕ викликають ChangeTracker
            // і НЕ спрацьовують EF-інтерцептори SaveChanges (наприклад, для автоматичного проставлення UpdatedAt).
            int updatedTom = db.Users
                .Where(u => u.Name == "Tom")
                .ExecuteUpdate(s => s
                    .SetProperty(u => u.Age, u => u.Age + 10)
                    .SetProperty(u => u.Name, _ => "Tomas"));
            Console.WriteLine($"Оновлено рядків (Tom -> Tomas, +10 років): {updatedTom}");

            foreach (var u in db.Users.AsNoTracking().ToList())
                Console.WriteLine($"  {u.Name} ({u.Age})");
        }

        // 2. Важливий нюанс: ChangeTracker не синхронізується автоматично!
        SqliteDbHelper.PrintSection("2. Розсинхронізація з уже відстежуваними об'єктами");
        using (var db = new AppDbContext())
        {
            var trackedAlice = db.Users.First(u => u.Name == "Alice");
            Console.WriteLine($"У пам'яті (до ExecuteUpdate): {trackedAlice.Name} ({trackedAlice.Age})");

            // Запит іде напряму в SQLite, минаючи ChangeTracker поточного DbContext
            db.Users.Where(u => u.Name == "Alice")
                .ExecuteUpdate(s => s.SetProperty(u => u.Age, _ => 50));

            // У trackedAlice значення Age все ще старе, бо ExecuteUpdate обходить ChangeTracker.
            // Якщо зараз викликати db.SaveChanges() зі змінами інших полів trackedAlice, можлива перезапис або конфлікт!
            Console.WriteLine($"У пам'яті (після ExecuteUpdate, без перезавантаження): {trackedAlice.Name} ({trackedAlice.Age})");

            // Перезавантаження актуального стану з БД через Entry().Reload() (або очищення кешу через db.ChangeTracker.Clear())
            db.Entry(trackedAlice).Reload();
            Console.WriteLine($"Після Reload() з БД: {trackedAlice.Name} ({trackedAlice.Age})");
        }

        // 3. ExecuteDelete — масове видалення одним SQL-запитом DELETE
        SqliteDbHelper.PrintSection("3. Масове видалення через ExecuteDelete");
        using (var db = new AppDbContext())
        {
            // Генерує DELETE FROM "Users" WHERE "Name" = 'Bob' без попереднього SELECT.
            // Увага: якщо на таблицю посилаються інші таблиці з каскадним видаленням на рівні БД (ON DELETE CASCADE),
            // воно спрацює на стороні SQLite (за умови PRAGMA foreign_keys = ON), але клієнтське каскадне видалення EF Core не виконується.
            int deleted = db.Users.Where(u => u.Name == "Bob").ExecuteDelete();
            Console.WriteLine($"Видалено рядків (Bob): {deleted}");

            Console.WriteLine("Залишились у БД:");
            foreach (var u in db.Users.AsNoTracking().ToList())
                Console.WriteLine($"  {u.Name} ({u.Age})");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_11
{
    /// <summary>Користувач (модель для демонстрації масових операцій ExecuteUpdate та ExecuteDelete).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.11 (ExecuteUpdate / ExecuteDelete).</summary>
    public class AppDbContext : DbContext
    {
        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Налаштовує підключення до локальної БД SQLite для уроку 5.11.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_11"));
        }
    }
}

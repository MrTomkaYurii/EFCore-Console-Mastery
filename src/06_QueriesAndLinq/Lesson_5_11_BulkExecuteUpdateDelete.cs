using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_11;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.11: Масове оновлення та видалення. ExecuteUpdate і ExecuteDelete.
/// Демонструє пряме виконання UPDATE та DELETE у SQLite без завантаження сутностей у пам'ять,
/// а також особливість відсутності синхронізації з ChangeTracker.
/// </summary>
public class Lesson_5_11_BulkExecuteUpdateDelete : ILesson
{
    public string Id => "5.11";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Масове оновлення та видалення. ExecuteUpdate і ExecuteDelete";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.11.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 11;

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
            // Збільшуємо Age на 1 для всіх користувачів одним SQL-запитом UPDATE
            int updatedAll = db.Users.ExecuteUpdate(s => s.SetProperty(u => u.Age, u => u.Age + 1));
            Console.WriteLine($"Оновлено рядків (усім +1 рік): {updatedAll}");

            // Оновлення кількох властивостей одночасно з умовою Where
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

            db.Users.Where(u => u.Name == "Alice")
                .ExecuteUpdate(s => s.SetProperty(u => u.Age, _ => 50));

            // У trackedAlice значення Age все ще старе, бо ExecuteUpdate обходить ChangeTracker
            Console.WriteLine($"У пам'яті (після ExecuteUpdate, без перезавантаження): {trackedAlice.Name} ({trackedAlice.Age})");

            // Перезавантаження з БД через Entry().Reload()
            db.Entry(trackedAlice).Reload();
            Console.WriteLine($"Після Reload() з БД: {trackedAlice.Name} ({trackedAlice.Age})");
        }

        // 3. ExecuteDelete — масове видалення одним SQL-запитом DELETE
        SqliteDbHelper.PrintSection("3. Масове видалення через ExecuteDelete");
        using (var db = new AppDbContext())
        {
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
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_11"));
        }
    }
}

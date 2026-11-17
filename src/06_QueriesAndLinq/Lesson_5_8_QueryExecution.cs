using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_8;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.8: Виконання запитів (відкладене та негайне виконання, Identity Resolution).
/// Показує, коли саме виконується SQL-запит та як ChangeTracker повертає той самий екземпляр об'єкта
/// для відстежуваних запитів і окремий — для AsNoTracking().
/// </summary>
public class Lesson_5_8_QueryExecution : ILesson
{
    public string Id => "5.8";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Виконання запитів (відкладене та негайне виконання)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.8.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 8;

    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);
        }

        // 1. Відкладене (Deferred) vs Негайне (Immediate) виконання
        SqliteDbHelper.PrintSection("1. Відкладене та негайне виконання запитів");
        using (var db = new AppDbContext())
        {
            // Відкладене виконання: тут SQL ще НЕ відправляється до БД
            IQueryable<User> deferredQuery = db.Users.Where(u => u.Id > 0);
            Console.WriteLine("Запит IQueryable сформовано (SQL ще не виконувався).");

            // Негайне виконання відбувається при виклику ToList(), FirstOrDefault(), Count() тощо
            var usersList = deferredQuery.ToList();
            int count = deferredQuery.Count();
            Console.WriteLine($"Негайне виконання ToList() та Count(): отримано {usersList.Count} (Count={count}).");
        }

        // 2. Identity Resolution для відстежуваних запитів
        SqliteDbHelper.PrintSection("2. Два відстежувані запити повертають одне й те саме посилання");
        using (var db = new AppDbContext())
        {
            var user1 = db.Users.FirstOrDefault();
            var user2 = db.Users.FirstOrDefault();

            if (user1 is not null && user2 is not null)
            {
                Console.WriteLine($"До зміни -> User1: {user1.Name}, User2: {user2.Name}");
                user1.Name = "Bob";
                // Обидві змінні посилаються на один і той самий об'єкт із кешу ChangeTracker
                Console.WriteLine($"Після зміни User1 -> User1: {user1.Name}, User2: {user2.Name}");
                Console.WriteLine($"ReferenceEquals(user1, user2) = {ReferenceEquals(user1, user2)}");
            }
        }

        // 3. Відстежуваний запит + AsNoTracking()
        SqliteDbHelper.PrintSection("3. Відстежуваний запит та AsNoTracking() створюють різні об'єкти");
        using (var db = new AppDbContext())
        {
            var user1 = db.Users.FirstOrDefault();
            var user2 = db.Users.AsNoTracking().FirstOrDefault();

            if (user1 is not null && user2 is not null)
            {
                Console.WriteLine($"До зміни -> User1: {user1.Name}, User2: {user2.Name}");
                user1.Name = "Bob";
                // user2 не входить до ChangeTracker, тому має власний незалежний екземпляр
                Console.WriteLine($"Після зміни User1 -> User1: {user1.Name}, User2: {user2.Name}");
                Console.WriteLine($"ReferenceEquals(user1, user2) = {ReferenceEquals(user1, user2)}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_8
{
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_8"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(new User { Id = 1, Name = "Tom" });
        }
    }
}

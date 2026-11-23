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
public class Lesson_5_08_QueryExecution : ILesson
{
    public string Id => "5.8";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Виконання запитів (відкладене та негайне виконання)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.8.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 8;

    /// <summary>
    /// Запускає демонстрацію відкладеного/негайного виконання запитів
    /// та механізму Identity Resolution у ChangeTracker (Tracking vs AsNoTracking).
    /// </summary>
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
            // Відкладене виконання: тут лише будується дерево виразів IQueryable<User>,
            // жодне з'єднання з SQLite ще не відкривається і SQL не відправляється.
            IQueryable<User> deferredQuery = db.Users.Where(u => u.Id > 0);
            Console.WriteLine("Запит IQueryable сформовано (SQL ще не виконувався).");

            // Негайне виконання (матеріалізація) відбувається при виклику ToList(), FirstOrDefault(), Count(), foreach тощо.
            // Зверніть увагу: виклик deferredQuery.ToList() і потім deferredQuery.Count() виконає ДВА окремі SQL-запити до БД!
            var usersList = deferredQuery.ToList();
            int count = deferredQuery.Count();
            Console.WriteLine($"Негайне виконання ToList() та Count(): отримано {usersList.Count} (Count={count}).");
        }

        // 2. Identity Resolution для відстежуваних запитів
        SqliteDbHelper.PrintSection("2. Два відстежувані запити повертають одне й те саме посилання");
        using (var db = new AppDbContext())
        {
            // Кожен FirstOrDefault() виконує SQL SELECT ... LIMIT 1, але при матеріалізації EF Core перевіряє,
            // чи вже є в ChangeTracker об'єкт із таким самим типом (User) та первинним ключем (Id = 1).
            var user1 = db.Users.FirstOrDefault();
            var user2 = db.Users.FirstOrDefault();

            if (user1 is not null && user2 is not null)
            {
                Console.WriteLine($"До зміни -> User1: {user1.Name}, User2: {user2.Name}");
                user1.Name = "Bob";
                // Завдяки Identity Resolution обидві змінні вказують на ОДИН екземпляр у пам'яті (ReferenceEquals == true).
                // Це гарантує узгодженість графу об'єктів у межах одного DbContext.
                Console.WriteLine($"Після зміни User1 -> User1: {user1.Name}, User2: {user2.Name}");
                Console.WriteLine($"ReferenceEquals(user1, user2) = {ReferenceEquals(user1, user2)}");
            }
        }

        // 3. Відстежуваний запит + AsNoTracking()
        SqliteDbHelper.PrintSection("3. Відстежуваний запит та AsNoTracking() створюють різні об'єкти");
        using (var db = new AppDbContext())
        {
            var user1 = db.Users.FirstOrDefault();
            // AsNoTracking() вимикає Identity Resolution із ChangeTracker: EF Core не шукає об'єкт у кеші
            // і створює новий екземпляр User (якщо ж у графі всередині одного AsNoTracking-запиту є дублікати,
            // для їх дедуплікації без трекінгу існує метод AsNoTrackingWithIdentityResolution()).
            var user2 = db.Users.AsNoTracking().FirstOrDefault();

            if (user1 is not null && user2 is not null)
            {
                Console.WriteLine($"До зміни -> User1: {user1.Name}, User2: {user2.Name}");
                user1.Name = "Bob";
                // user2 не входить до ChangeTracker, тому має власний незалежний екземпляр у пам'яті
                Console.WriteLine($"Після зміни User1 -> User1: {user1.Name}, User2: {user2.Name}");
                Console.WriteLine($"ReferenceEquals(user1, user2) = {ReferenceEquals(user1, user2)}");
            }
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_8
{
    /// <summary>Користувач (модель для демонстрації виконання запитів та Identity Resolution).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.8 із початковим записом через HasData.</summary>
    public class AppDbContext : DbContext
    {
        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Налаштовує підключення до локальної БД SQLite для уроку 5.8.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_8"));
        }

        /// <summary>Додає початковий рядок User(Id = 1, Name = "Tom") під час створення БД.</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(new User { Id = 1, Name = "Tom" });
        }
    }
}

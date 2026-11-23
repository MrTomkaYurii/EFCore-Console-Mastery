using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_6;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.6: IEnumerable та IQueryable.
/// Демонструє принципову різницю між фільтрацією в пам'яті (IEnumerable)
/// та побудовою SQL-виразу на стороні сервера БД (IQueryable).
/// </summary>
public class Lesson_5_06_IEnumerableVsIQueryable : ILesson
{
    public string Id => "5.6";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "IEnumerable та IQueryable";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.6.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 9;

    /// <summary>
    /// Запускає демонстрацію різниці між <see cref="IEnumerable{T}"/> (фільтрація у пам'яті клієнта)
    /// та <see cref="IQueryable{T}"/> (побудова SQL-запиту на стороні SQLite).
    /// </summary>
    public void Run()
    {
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);
        }

        // 1. IEnumerable<User>: завантажує всю таблицю, фільтрує у пам'яті клієнта
        SqliteDbHelper.PrintSection("1. IEnumerable<User> — фільтрація на стороні клієнта");
        using (var db = new AppDbContext())
        {
            // Неявне приведення DbSet<User> (який реалізує IQueryable<User>) до IEnumerable<User>
            // змінює перевантаження методу Where на етапі компіляції!
            IEnumerable<User> userIEnum = db.Users;

            // Where тут — це Enumerable.Where(Func<User, bool>), а не Queryable.Where(Expression<Func<User, bool>>)!
            // Через це EF Core не бачить умову u.Id < 10, завантажує УСІ рядки з таблиці Users (SELECT "u"."Id", "u"."Name" FROM "Users" AS "u"),
            // матеріалізує їх у пам'ять (і додає в ChangeTracker), і лише потім C# відфільтровує елементи.
            // На великих таблицях (мільйони рядків) це призводить до катастрофічного споживання пам'яті та мережі.
            var users = userIEnum.Where(u => u.Id < 10).ToList();

            Console.WriteLine($"Отримано записів (IEnumerable): {users.Count}");
            foreach (var u in users)
                Console.WriteLine($"  {u.Id}. {u.Name}");
        }

        // 2. IQueryable<User>: фільтрація транслюється у SQL WHERE
        SqliteDbHelper.PrintSection("2. IQueryable<User> — фільтрація на стороні БД (SQL WHERE)");
        using (var db = new AppDbContext())
        {
            IQueryable<User> userIQuer = db.Users;

            // Queryable.Where приймає Expression<Func<User, bool>> (дерево виразів) і додає умову до AST запиту.
            // Запит ще не виконується (відкладене виконання), а SQLite отримає SELECT ... WHERE "u"."Id" < 10.
            var query = userIQuer.Where(u => u.Id < 10);

            Console.WriteLine("Згенерований SQL:");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();

            foreach (var u in query.ToList())
                Console.WriteLine($"  {u.Id}. {u.Name}");
        }

        // 3. Динамічне нашарування умов на IQueryable
        SqliteDbHelper.PrintSection("3. Динамічна побудова запиту через IQueryable");
        using (var db = new AppDbContext())
        {
            // Зручний патерн для фільтрів пошуку (наприклад, в API або UI):
            // доки ми працюємо з IQueryable<User>, ми можемо умовно (через if) додавати .Where(), .OrderBy(), .Skip().Take(),
            // і EF Core об'єднає всі умови в один оптимальний SQL-запит через AND перед єдиним зверненням до БД у ToList().
            IQueryable<User> dynamicQuery = db.Users;
            dynamicQuery = dynamicQuery.Where(u => u.Id < 10);
            dynamicQuery = dynamicQuery.Where(u => u.Name == "Tom");

            Console.WriteLine("Згенерований SQL для комбінованого запиту:");
            Console.WriteLine(dynamicQuery.ToQueryString());
            Console.WriteLine();

            foreach (var u in dynamicQuery.ToList())
                Console.WriteLine($"  Знайдено: {u.Id}. {u.Name}");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_6
{
    /// <summary>Користувач (проста модель для демонстрації IEnumerable vs IQueryable).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.6 із початковим заповненням (HasData).</summary>
    public class AppDbContext : DbContext
    {
        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Налаштовує підключення до локальної БД SQLite для уроку 5.6.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_6"));
        }

        /// <summary>Налаштовує модель та додає початкові дані (Data Seeding) через HasData.</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // HasData вимагає явного задання первинного ключа Id; при EnsureCreated() / міграціях
            // ці записи автоматично вставляються в таблицю Users.
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "Tom" },
                new User { Id = 2, Name = "Bob" },
                new User { Id = 15, Name = "Alice" }
            );
        }
    }
}

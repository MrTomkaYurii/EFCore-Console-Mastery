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
public class Lesson_5_6_IEnumerableVsIQueryable : ILesson
{
    public string Id => "5.6";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "IEnumerable та IQueryable";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.6.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 9;

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
            IEnumerable<User> userIEnum = db.Users;
            // Where належить Enumerable.Where (Func<User, bool>), а не Queryable.Where!
            // EF Core завантажить УСІ рядки з таблиці Users (SELECT Id, Name FROM Users), і лише потім відфільтрує в C#
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
            // Where приймає Expression<Func<User, bool>> і додає умову до дерева виразів
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
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_6"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "Tom" },
                new User { Id = 2, Name = "Bob" },
                new User { Id = 15, Name = "Alice" }
            );
        }
    }
}

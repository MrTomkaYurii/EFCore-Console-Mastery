using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_9;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 5.9: Фільтри запитів рівня моделі (HasQueryFilter).
/// Демонструє автоматичне застосування глобального фільтра в OnModelCreating
/// та його тимчасове відключення через IgnoreQueryFilters().
/// </summary>
public class Lesson_5_9_GlobalQueryFilters : ILesson
{
    public string Id => "5.9";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Фільтри запитів рівня моделі (HasQueryFilter)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.9.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 10;

    public void Run()
    {
        // 1. Наповнення бази даних
        using (var db = new AppDbContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            Role userRole = new() { Name = "user" };   // Id = 1
            Role adminRole = new() { Name = "admin" }; // Id = 2
            db.Roles.AddRange(userRole, adminRole);

            db.Users.AddRange(
                new User { Name = "Tom", Age = 17, Role = userRole },
                new User { Name = "Bob", Age = 18, Role = userRole },
                new User { Name = "Alice", Age = 19, Role = adminRole },
                new User { Name = "Sam", Age = 20, Role = adminRole }
            );
            db.SaveChanges();
        }

        // 2. Автоматичне застосування HasQueryFilter (Age > 17 && RoleId == CurrentRoleId)
        SqliteDbHelper.PrintSection("1. Запит із активним HasQueryFilter (CurrentRoleId = 2)");
        using (var db = new AppDbContext { CurrentRoleId = 2 })
        {
            var query = db.Users.Include(u => u.Role);
            Console.WriteLine("Згенерований SQL (умова WHERE додана автоматично):");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();

            foreach (var user in query.ToList())
                Console.WriteLine($"  Name: {user.Name}, Age: {user.Age}, Role: {user.Role?.Name}");

            // Фільтр також діє на агрегатні операції (Min, Count тощо)
            int minFilteredAge = db.Users.Min(u => u.Age);
            Console.WriteLine($"\nМінімальний вік з фільтром: {minFilteredAge}");
        }

        // 3. Ігнорування глобального фільтра через IgnoreQueryFilters()
        SqliteDbHelper.PrintSection("2. Відключення фільтра через IgnoreQueryFilters()");
        using (var db = new AppDbContext { CurrentRoleId = 2 })
        {
            int minAllAge = db.Users.IgnoreQueryFilters().Min(u => u.Age);
            Console.WriteLine($"Мінімальний вік без фільтра (IgnoreQueryFilters): {minAllAge}");

            foreach (var user in db.Users.IgnoreQueryFilters().Include(u => u.Role).ToList())
                Console.WriteLine($"  Name: {user.Name}, Age: {user.Age}, Role: {user.Role?.Name}");
        }
    }
}
}

namespace EFCoreConsoleMastery.Topics.Ch06.L5_9
{
    public class Role
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public int RoleId { get; set; }
        public Role? Role { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;

        /// <summary>Параметр рівня контексту, що використовується у глобальному фільтрі.</summary>
        public int CurrentRoleId { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch06_lesson_5_9"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Фільтр рівня моделі застосовується до всіх LINQ-запитів до User
            modelBuilder.Entity<User>()
                .HasQueryFilter(u => u.Age > 17 && u.RoleId == CurrentRoleId);
        }
    }
}

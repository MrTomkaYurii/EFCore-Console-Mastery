using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Topics.Ch06.L5_9;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch06_QueriesAndLinq
{
/// <summary>
/// Урок 6.10: Фільтри запитів рівня моделі (HasQueryFilter).
/// Демонструє автоматичне застосування глобального фільтра в OnModelCreating
/// та його тимчасове відключення через IgnoreQueryFilters().
/// </summary>
public class Lesson_6_10_GlobalQueryFilters : ILesson
{
    public string Id => "6.10";
    public string Chapter => "06. Запити та LINQ to Entities";
    public string Title => "Фільтри запитів рівня моделі (HasQueryFilter)";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/5.9.php";
    public int ChapterOrder => 6;
    public int LessonOrder => 10;

    /// <summary>
    /// Запускає демонстрацію глобальних фільтрів запитів (<c>HasQueryFilter</c>)
    /// із динамічним параметром контексту (<see cref="AppDbContext.CurrentRoleId"/>) та їх відключення через <c>IgnoreQueryFilters()</c>.
    /// </summary>
    public void Run()
    {
        // 1. Наповнення бази даних
        using (var db = new AppDbContext())
        {
            SqlServerDbHelper.RecreateSchema(db, "l6_10");

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
        SqlServerDbHelper.PrintSection("1. Запит із активним HasQueryFilter (CurrentRoleId = 2)");
        // Прив'язка фільтра до властивості екземпляра DbContext (CurrentRoleId) є типовим прийомом для
        // реалізації Soft Delete (IsDeleted == false) або Multi-Tenancy (TenantId == _currentTenantId).
        // EF Core параметризує CurrentRoleId у SQLite як @__ef_filter__CurrentRoleId_0, тому план запиту кешується,
        // а значення підставляється динамічно для кожного екземпляра AppDbContext!
        using (var db = new AppDbContext { CurrentRoleId = 2 })
        {
            var query = db.Users.Include(u => u.Role);
            Console.WriteLine("Згенерований SQL (умова WHERE додана автоматично):");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine();

            foreach (var user in query.ToList())
                Console.WriteLine($"  Name: {user.Name}, Age: {user.Age}, Role: {user.Role?.Name}");

            // Глобальний фільтр діє на УСІ LINQ-операції: вибірки, Include, Find (у EF Core), агрегати (Min, Count),
            // а також на ExecuteUpdate / ExecuteDelete!
            int minFilteredAge = db.Users.Min(u => u.Age);
            Console.WriteLine($"\nМінімальний вік з фільтром: {minFilteredAge}");
        }

        // 3. Ігнорування глобального фільтра через IgnoreQueryFilters()
        SqlServerDbHelper.PrintSection("2. Відключення фільтра через IgnoreQueryFilters()");
        using (var db = new AppDbContext { CurrentRoleId = 2 })
        {
            // IgnoreQueryFilters() вимикає ВСІ глобальні фільтри для поточного запиту (корисно для адмін-панелей,
            // відновлення видалених записів або фонових завдань).
            // Зауваження: для однієї сутності можна задати лише ОДИН виклик HasQueryFilter (наступний перезапише попередній),
            // тому кілька умов об'єднують через &&.
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
    /// <summary>Роль користувача (довідникова сутність).</summary>
    public class Role
    {
        /// <summary>Первинний ключ ролі.</summary>
        public int Id { get; set; }

        /// <summary>Назва ролі (наприклад, "user", "admin").</summary>
        public string? Name { get; set; }
    }

    /// <summary>Користувач (до цієї сутності застосовується глобальний фільтр запитів).</summary>
    public class User
    {
        /// <summary>Первинний ключ користувача.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача.</summary>
        public string? Name { get; set; }

        /// <summary>Вік користувача.</summary>
        public int Age { get; set; }

        /// <summary>Зовнішній ключ до таблиці Roles.</summary>
        public int RoleId { get; set; }

        /// <summary>Навігаційна властивість до ролі користувача.</summary>
        public Role? Role { get; set; }
    }

    /// <summary>Контекст бази даних для уроку 5.9 (глобальні фільтри запитів HasQueryFilter).</summary>
    public class AppDbContext : DbContext
    {
        /// <summary>Таблиця користувачів.</summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>Таблиця ролей.</summary>
        public DbSet<Role> Roles { get; set; } = null!;

        /// <summary>Параметр рівня екземпляра контексту, що динамічно підставляється у глобальний фільтр.</summary>
        public int CurrentRoleId { get; set; }

        /// <summary>Налаштовує підключення до локальної БД SQLite для уроку 5.9.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        /// <summary>Реєструє глобальний фільтр запитів для сутності <see cref="User"/>.</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l6_10");
            // Фільтр рівня моделі застосовується до всіх LINQ-запитів до User.
            // Звернення до властивості екземпляра CurrentRoleId транслятор перетворює на SQL-параметр.
            modelBuilder.Entity<User>()
                .HasQueryFilter(u => u.Age > 17 && u.RoleId == CurrentRoleId);
        }
    }
}

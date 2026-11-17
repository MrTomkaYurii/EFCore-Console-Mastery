using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_6;

    /// <summary>
    /// Урок 3.6: Відношення багато до багатьох (Many-to-Many) та налаштування проміжної таблиці через UsingEntity.
    /// </summary>
    public class Lesson_3_6_ManyToMany : ILesson
    {
        public string Id => "3.6";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Відношення багато до багатьох";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.6.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 9;

        public void Run()
        {
            SqliteDbHelper.PrintSection("1. Створення зв'язку M:N без явного проміжного класу");
            using (var db = new AppContextL3_6())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var tom = new Student { Name = "Tom" };
                var alice = new Student { Name = "Alice" };
                var bob = new Student { Name = "Bob" };

                var algorithms = new Course { Name = "Алгоритми" };
                var basics = new Course { Name = "Основи програмування" };

                // Додаємо курси до студентів — EF Core автоматично заповнить проміжну таблицю "Enrollments"
                tom.Courses.AddRange(new[] { algorithms, basics });
                alice.Courses.Add(algorithms);
                bob.Courses.Add(basics);

                db.Students.AddRange(tom, alice, bob);
                db.Courses.AddRange(algorithms, basics);
                db.SaveChanges();
            }

            SqliteDbHelper.PrintSection("2. Читання курсів та записаних студентів");
            using (var db = new AppContextL3_6())
            {
                var courses = db.Courses.Include(c => c.Students).ToList();
                foreach (var c in courses)
                {
                    Console.WriteLine($"Курс '{c.Name}': {string.Join(", ", c.Students.Select(s => s.Name))}");
                }
            }

            SqliteDbHelper.PrintSection("3. Оновлення зв'язків (відписка та підписка на курс)");
            using (var db = new AppContextL3_6())
            {
                var alice = db.Students.Include(s => s.Courses).First(s => s.Name == "Alice");
                var algorithms = db.Courses.First(c => c.Name == "Алгоритми");
                var basics = db.Courses.First(c => c.Name == "Основи програмування");

                // Видаляємо один курс і додаємо інший
                alice.Courses.Remove(algorithms);
                alice.Courses.Add(basics);
                db.SaveChanges();
            }

            using (var db = new AppContextL3_6())
            {
                Console.WriteLine("Після переведення Alice з 'Алгоритми' на 'Основи програмування':");
                foreach (var c in db.Courses.Include(c => c.Students).ToList())
                {
                    Console.WriteLine($"Курс '{c.Name}': {string.Join(", ", c.Students.Select(s => s.Name))}");
                }
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_6
{
    public class Course
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<Student> Students { get; set; } = new();
    }

    public class Student
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<Course> Courses { get; set; } = new();
    }

    public class AppContextL3_6 : DbContext
    {
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_6"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Перевизначаємо назву автоматично згенерованої проміжної таблиці на "Enrollments"
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Students)
                .WithMany(s => s.Courses)
                .UsingEntity(j => j.ToTable("Enrollments"));
        }
    }
}

using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_6;

    /// <summary>
    /// Урок 4.9: Відношення багато до багатьох (Many-to-Many) та налаштування проміжної таблиці через UsingEntity.
    /// </summary>
    public class Lesson_4_09_ManyToMany : ILesson
    {
        public string Id => "4.9";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Відношення багато до багатьох";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.6.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 9;

        /// <summary>
        /// Демонструє неявний зв'язок багато-до-багатьох (M:N) без окремого C#-класу проміжної сутності,
        /// а також перевизначення назви проміжної таблиці через <c>UsingEntity</c>.
        /// </summary>
        public void Run()
        {
            SqlServerDbHelper.PrintSection("1. Створення зв'язку M:N без явного проміжного класу");
            using (var db = new AppContextL3_6())
            {
                SqlServerDbHelper.RecreateSchema(db, "l4_09");

                var tom = new Student { Name = "Tom" };
                var alice = new Student { Name = "Alice" };
                var bob = new Student { Name = "Bob" };

                var algorithms = new Course { Name = "Алгоритми" };
                var basics = new Course { Name = "Основи програмування" };

                // Додаємо курси до колекцій студентів — EF Core автоматично створить рядки у тіньовій (shadow)
                // проміжній таблиці "Enrollments" зі складеним первинним ключем (CoursesId, StudentsId).
                tom.Courses.AddRange(new[] { algorithms, basics });
                alice.Courses.Add(algorithms);
                bob.Courses.Add(basics);

                db.Students.AddRange(tom, alice, bob);
                db.Courses.AddRange(algorithms, basics);
                db.SaveChanges();
            }

            SqlServerDbHelper.PrintSection("2. Читання курсів та записаних студентів");
            using (var db = new AppContextL3_6())
            {
                // Include(c => c.Students) генерує подвійний LEFT JOIN: Courses -> Enrollments -> Students.
                var courses = db.Courses.Include(c => c.Students).ToList();
                foreach (var c in courses)
                {
                    Console.WriteLine($"Курс '{c.Name}': {string.Join(", ", c.Students.Select(s => s.Name))}");
                }
            }

            SqlServerDbHelper.PrintSection("3. Оновлення зв'язків (відписка та підписка на курс)");
            using (var db = new AppContextL3_6())
            {
                // Важливо: щоб Remove() / Add() у колекції M:N коректно відстежилися Change Tracker'ом,
                // колекцію alice.Courses потрібно завантажити заздалегідь через Include().
                var alice = db.Students.Include(s => s.Courses).First(s => s.Name == "Alice");
                var algorithms = db.Courses.First(c => c.Name == "Алгоритми");
                var basics = db.Courses.First(c => c.Name == "Основи програмування");

                // Видалення курсу з колекції генерує DELETE FROM "Enrollments", а додавання — INSERT INTO "Enrollments".
                // Самі записи в таблицях Students та Courses залишаються недоторканими.
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
    /// <summary>Навчальний курс (сторона M у зв'язку M:N).</summary>
    public class Course
    {
        /// <summary>Первинний ключ курсу.</summary>
        public int Id { get; set; }

        /// <summary>Назва курсу.</summary>
        public string? Name { get; set; }

        /// <summary>Колекція студентів, записаних на курс (skip navigation).</summary>
        public List<Student> Students { get; set; } = new();
    }

    /// <summary>Студент (сторона N у зв'язку M:N).</summary>
    public class Student
    {
        /// <summary>Первинний ключ студента.</summary>
        public int Id { get; set; }

        /// <summary>Ім'я студента.</summary>
        public string? Name { get; set; }

        /// <summary>Колекція курсів, на які записаний студент (skip navigation).</summary>
        public List<Course> Courses { get; set; } = new();
    }

    /// <summary>Контекст бази даних для уроку 3.6 (SQLite).</summary>
    public class AppContextL3_6 : DbContext
    {
        /// <summary>Курси.</summary>
        public DbSet<Course> Courses { get; set; } = null!;

        /// <summary>Студенти.</summary>
        public DbSet<Student> Students { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        }

        /// <summary>
        /// Налаштовує проміжну таблицю зв'язку багато-до-багатьох.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("l4_09");
            // Без UsingEntity EF Core назвав би проміжну таблицю "CourseStudent" (за алфавітом назв класів).
            // UsingEntity(j => j.ToTable("Enrollments")) змінює лише ім'я таблиці в SQLite, залишаючи її без окремого C#-класу
            // (під капотом використовується Dictionary<string, object> — property bag).
            // Якщо в проміжній таблиці потрібні додаткові поля (наприклад, Grade або EnrolledAt), варто створити явний клас Enrollment.
            modelBuilder.Entity<Course>()
                .HasMany(c => c.Students)
                .WithMany(s => s.Courses)
                .UsingEntity(j => j.ToTable("Enrollments"));
        }
    }
}

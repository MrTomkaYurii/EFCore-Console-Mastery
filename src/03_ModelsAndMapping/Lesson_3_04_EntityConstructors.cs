using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch03
{

/// <summary>
/// Урок 3.4: Конструктори сутностей.
/// Показує, як EF Core використовує параметризовані конструктори (у тому числі private)
/// під час матеріалізації об'єктів із бази даних, а також обмеження щодо навігаційних властивостей.
/// </summary>
public class Lesson_3_04_EntityConstructors : ILesson
{
    public string Id => "3.4";
    public string Chapter => "03. Створення моделей в EF Core";
    public string Title => "Конструктори сутностей";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.10.php";
    public int ChapterOrder => 3;
    public int LessonOrder => 4;

    /// <summary>
    /// Запускає демонстрацію роботи конструкторів сутностей:
    /// 1) створює та зберігає сутності через параметризовані конструктори;
    /// 2) читає їх у новому контексті через Include(), показуючи, як EF Core викликає
    /// публічні та приватні конструктори під час матеріалізації рядків із SQLite.
    /// </summary>
    public void Run()
    {
        SqliteDbHelper.PrintSection("3.4. Конструктори сутностей");

        // Крок 1: Створення та збереження даних у першому контексті
        using (var db = new L2_10.ApplicationContext())
        {
            SqliteDbHelper.RecreateDatabase(db);

            var company = new L2_10.Company("JetBrains");
            var tom = new L2_10.User("Tom", 37) { Company = company };
            var bob = new L2_10.User("Bob", 41) { Company = company };

            db.Users.AddRange(tom, bob);
            db.SaveChanges();
        }

        // Крок 2: Читання з БД у новому контексті — EF Core викликає конструктори при матеріалізації.
        // Важливо: для Company спрацює private Company(), а потім EF Core заповнить Id та Name через властивості;
        // для User спрацює User(string name, int age), а потім EF Core встановить Id та навігацію Company.
        Console.WriteLine("\nОтримання даних з БД (EF Core викликає конструктори сутностей):");
        using (var db = new L2_10.ApplicationContext())
        {
            // Include(u => u.Company) генерує LEFT JOIN "Companies" ON "Users"."CompanyId" = "Companies"."Id".
            var users = db.Users.Include(u => u.Company).ToList();
            foreach (var user in users)
            {
                Console.WriteLine($"  Id={user.Id}, Name={user.Name}, Age={user.Age}, Company={user.Company?.Name}");
            }
        }
    }
}

}

namespace EFCoreConsoleMastery.Topics.Ch03.L2_10
{
    /// <summary>
    /// Сутність користувача з параметризованим конструктором (без конструктора за замовчуванням).
    /// </summary>
    public class User
    {
        /// <summary>Первинний ключ (встановлюється EF Core через set-аксесор після виклику конструктора).</summary>
        public int Id { get; set; }

        /// <summary>Ім'я користувача (передається в конструктор User(string name, int age)).</summary>
        public string Name { get; set; }

        /// <summary>Вік користувача (передається в конструктор User(string name, int age)).</summary>
        public int Age { get; set; }

        /// <summary>
        /// Навігаційна властивість до компанії.
        /// Важливе обмеження EF Core: навігаційні властивості (Company, колекції тощо)
        /// НЕ можуть передаватися як параметри конструктора — EF Core заповнює їх після створення об'єкта.
        /// </summary>
        public Company? Company { get; set; }

        /// <summary>
        /// Параметризований конструктор: імена параметрів (name, age) збігаються з властивостями
        /// моделі (Name, Age) без урахування регістру.
        /// Id у конструкторі немає — EF Core встановить його напряму через властивість після матеріалізації.
        /// </summary>
        public User(string name, int age)
        {
            Name = name;
            Age = age;
            Console.WriteLine($"  [Конструктор User(string, int)] Створено об'єкт {name}, {age}");
        }
    }

    /// <summary>
    /// Сутність компанії, що має як публічний параметризований конструктор, так і приватний без параметрів.
    /// </summary>
    public class Company
    {
        /// <summary>Первинний ключ компанії.</summary>
        public int Id { get; set; }

        /// <summary>Назва компанії.</summary>
        public string Name { get; set; }

        /// <summary>Колекційна навігаційна властивість користувачів цієї компанії (один-до-багатьох).</summary>
        public ICollection<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// Приватний конструктор без параметрів.
        /// EF Core може викликати навіть private або protected конструктори через reflection.
        /// Якщо в класі є і безпараметровий, і параметризований конструктор, EF Core віддає перевагу
        /// безпараметровому (тому при читанні з БД тут не друкується повідомлення з Company(string)).
        /// </summary>
        private Company()
        {
            Name = string.Empty;
        }

        /// <summary>Публічний конструктор для створення компанії в прикладному коді.</summary>
        public Company(string name)
        {
            Name = name;
            Console.WriteLine($"  [Конструктор Company(string)] Створено компанію {name}");
        }
    }

    /// <summary>
    /// Контекст бази даних для уроку 2.10.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        /// <summary>Набір користувачів (таблиця "Users").</summary>
        public DbSet<User> Users => Set<User>();

        /// <summary>Набір компаній (таблиця "Companies").</summary>
        public DbSet<Company> Companies => Set<Company>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch03_l2_10"));
        }
    }
}

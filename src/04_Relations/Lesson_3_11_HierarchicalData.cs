using EFCoreConsoleMastery.Core;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch04
{
    using EFCoreConsoleMastery.Topics.Ch04.L3_11;

    /// <summary>
    /// Урок 3.11: Ієрархічні (деревоподібні) дані на прикладі самопосилальної сутності MenuItem.
    /// </summary>
    public class Lesson_3_11_HierarchicalData : ILesson
    {
        public string Id => "3.11";
        public string Chapter => "04. Відносини між моделями";
        public string Title => "Ієрархічні дані";
        public string MetanitUrl => "https://metanit.com/sharp/efcore/3.11.php";
        public int ChapterOrder => 4;
        public int LessonOrder => 12;

        /// <summary>
        /// Демонструє роботу з ієрархічними (деревоподібними) даними через самопосилальну таблицю (self-referencing table)
        /// та автоматичну збірку графа в пам'яті за допомогою Relationship Fixup.
        /// </summary>
        public void Run()
        {
            using (var db = new AppContextL3_11())
            {
                SqliteDbHelper.RecreateDatabase(db);

                var file = new MenuItem { Title = "File" };
                var edit = new MenuItem { Title = "Edit" };
                var open = new MenuItem { Title = "Open", Parent = file };
                var save = new MenuItem { Title = "Save", Parent = file };
                var saveAs = new MenuItem { Title = "Save As...", Parent = save };
                var copy = new MenuItem { Title = "Copy", Parent = edit };
                var paste = new MenuItem { Title = "Paste", Parent = edit };

                // EF Core автоматично розставляє порядок INSERT: спочатку корені (file, edit), потім 2-й рівень (open, save, copy, paste),
                // потім 3-й рівень (saveAs), щоб не порушити FOREIGN KEY ("ParentId") REFERENCES "MenuItems" ("Id").
                db.MenuItems.AddRange(file, edit, open, save, saveAs, copy, paste);
                db.SaveChanges();
            }

            SqliteDbHelper.PrintSection("1. Завантаження всього дерева в контекст (Relationship Fix-up)");
            using (var db = new AppContextL3_11())
            {
                // Для дерев довільної глибини один Include(m => m.Children) завантажить лише 1 рівень.
                // Завантаживши всю таблицю одним запитом SELECT * FROM MenuItems у відстежуваний контекст,
                // EF Core через Relationship Fixup сам зв'яже всі Parent <-> Children на будь-якій глибині.
                // Примітка: з AsNoTracking() автоматичний fixup для вже завантажених колекцій працюватиме обмежено, тому тут tracking потрібен.
                var allItems = db.MenuItems.ToList();
                var roots = allItems.Where(m => m.ParentId == null).ToList();

                foreach (var root in roots)
                {
                    PrintTree(root, 0);
                }
            }

            SqliteDbHelper.PrintSection("2. Вибіркове завантаження конкретного пункту з підменю через Include");
            using (var db = new AppContextL3_11())
            {
                // Include(m => m.Children) завантажує лише безпосередніх нащадків (1 рівень: Open, Save),
                // без 3-го рівня (Save As...). Для фіксованої глибини використовують ланцюжок ThenInclude.
                var fileMenu = db.MenuItems
                    .Include(m => m.Children)
                    .FirstOrDefault(m => m.Title == "File");

                if (fileMenu != null)
                {
                    Console.WriteLine($"{fileMenu.Title}: {string.Join(", ", fileMenu.Children.Select(c => c.Title))}");
                }
            }
        }

        private static void PrintTree(MenuItem item, int level)
        {
            Console.WriteLine($"{new string(' ', level * 2)}- {item.Title}");
            foreach (var child in item.Children)
            {
                PrintTree(child, level + 1);
            }
        }
    }
}

namespace EFCoreConsoleMastery.Topics.Ch04.L3_11
{
    /// <summary>
    /// Самопосилальна (self-referencing) сутність для побудови дерева довільної вкладеності (Adjacency List).
    /// </summary>
    public class MenuItem
    {
        /// <summary>Первинний ключ пункту меню.</summary>
        public int Id { get; set; }

        /// <summary>Назва пункту меню.</summary>
        public string? Title { get; set; }

        /// <summary>
        /// Nullable зовнішній ключ на батьківський елемент тієї ж таблиці MenuItems (<c>null</c> для кореневих пунктів).
        /// Оскільки FK nullable, за замовчуванням діє <c>ClientSetNull</c> (без каскадного видалення в БД).
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>Навігаційна властивість до батьківського пункту меню.</summary>
        public MenuItem? Parent { get; set; }

        /// <summary>Колекція дочірніх підпунктів поточного вузла.</summary>
        public List<MenuItem> Children { get; set; } = new();
    }

    /// <summary>Контекст бази даних для уроку 3.11 (SQLite).</summary>
    public class AppContextL3_11 : DbContext
    {
        /// <summary>Пункти меню (ієрархічна таблиця).</summary>
        public DbSet<MenuItem> MenuItems { get; set; } = null!;

        /// <summary>Налаштовує підключення до SQLite.</summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_11"));
        }
    }
}

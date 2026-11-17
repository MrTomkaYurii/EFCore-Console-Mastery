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

                db.MenuItems.AddRange(file, edit, open, save, saveAs, copy, paste);
                db.SaveChanges();
            }

            SqliteDbHelper.PrintSection("1. Завантаження всього дерева в контекст (Relationship Fix-up)");
            using (var db = new AppContextL3_11())
            {
                // Коли всі елементи завантажені в контекст, EF Core автоматично будує зв'язки Parent <-> Children
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
    /// Самопосилальна сутність для побудови дерева будь-якої вкладеності.
    /// </summary>
    public class MenuItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }

        /// <summary>Nullable зовнішній ключ на батьківський елемент (null для кореневих пунктів).</summary>
        public int? ParentId { get; set; }
        public MenuItem? Parent { get; set; }

        /// <summary>Дочірні елементи поточного вузла.</summary>
        public List<MenuItem> Children { get; set; } = new();
    }

    public class AppContextL3_11 : DbContext
    {
        public DbSet<MenuItem> MenuItems { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(SqliteDbHelper.GetConnectionString("ch04_l3_11"));
        }
    }
}

using System.Reflection;
using System.Text;
using EFCoreConsoleMastery.Core;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Автоматично завантажуємо всі збірки глав (EFCoreMastery.Ch*.dll) із каталогу запуску
// та знаходимо всі уроки, що реалізують ILesson
var assemblies = Directory
    .GetFiles(AppContext.BaseDirectory, "EFCoreMastery.Ch*.dll")
    .Select(Assembly.LoadFrom)
    .Append(Assembly.GetExecutingAssembly())
    .Distinct()
    .ToList();

var lessons = assemblies
    .SelectMany(a => a.GetTypes())
    .Where(t => typeof(ILesson).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
    .Select(t => (ILesson)Activator.CreateInstance(t)!)
    .OrderBy(l => l.ChapterOrder)
    .ThenBy(l => l.LessonOrder)
    .ToList();

if (args.Length > 0)
{
    if (args[0] == "--all")
    {
        RunAllLessons(lessons);
        return;
    }

    var target = lessons.FirstOrDefault(l => l.Id.Equals(args[0], StringComparison.OrdinalIgnoreCase));
    if (target is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Урок з ID '{args[0]}' не знайдено.");
        Console.ResetColor();
        return;
    }

    ExecuteLesson(target);
    return;
}

RunInteractiveMenu(lessons);

static void RunInteractiveMenu(List<ILesson> lessons)
{
    var chapters = lessons
        .GroupBy(l => (l.ChapterOrder, l.Chapter))
        .OrderBy(g => g.Key.ChapterOrder)
        .ToList();

    while (true)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("============================================================");
        Console.WriteLine("   EFCore-Console-Mastery (SQLite + Metanit EF Core Guide)  ");
        Console.WriteLine("============================================================");
        Console.ResetColor();
        Console.WriteLine($"Знайдено глав: {chapters.Count} | Усього уроків: {lessons.Count}");
        Console.WriteLine();

        foreach (var ch in chapters)
        {
            Console.WriteLine($"  [{ch.Key.ChapterOrder}] {ch.Key.Chapter} ({ch.Count()} тем)");
        }

        Console.WriteLine();
        Console.WriteLine("Введіть номер глави (1-8), ID уроку (напр. 1.4), 'all' або '0' для виходу:");
        Console.Write("> ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input) || input == "0")
            break;

        if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            RunAllLessons(lessons);
            Pause();
            continue;
        }

        // Якщо введено ID уроку напряму (напр. "1.4")
        var directLesson = lessons.FirstOrDefault(l => l.Id.Equals(input, StringComparison.OrdinalIgnoreCase));
        if (directLesson is not null)
        {
            ExecuteLesson(directLesson);
            Pause();
            continue;
        }

        if (int.TryParse(input, out int chapterNum))
        {
            var selectedChapter = chapters.FirstOrDefault(c => c.Key.ChapterOrder == chapterNum);
            if (selectedChapter is not null)
            {
                RunChapterMenu(selectedChapter.Key.Chapter, selectedChapter.ToList());
            }
        }
    }
}

static void RunChapterMenu(string chapterTitle, List<ILesson> chapterLessons)
{
    while (true)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== {chapterTitle} ===");
        Console.ResetColor();
        Console.WriteLine();

        for (int i = 0; i < chapterLessons.Count; i++)
        {
            var l = chapterLessons[i];
            Console.WriteLine($"  {i + 1,2}. [{l.Id}] {l.Title}");
        }

        Console.WriteLine();
        Console.WriteLine("Оберіть номер у списку або '0' для повернення назад:");
        Console.Write("> ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input) || input == "0")
            break;

        if (int.TryParse(input, out int idx) && idx >= 1 && idx <= chapterLessons.Count)
        {
            ExecuteLesson(chapterLessons[idx - 1]);
            Pause();
        }
    }
}

static void ExecuteLesson(ILesson lesson)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("------------------------------------------------------------");
    Console.WriteLine($"Урок [{lesson.Id}]: {lesson.Title}");
    Console.WriteLine($"Глава: {lesson.Chapter}");
    Console.WriteLine($"Джерело: {lesson.MetanitUrl}");
    Console.WriteLine("------------------------------------------------------------");
    Console.ResetColor();

    try
    {
        lesson.Run();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[OK] Урок [{lesson.Id}] виконано успішно.");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[ПОМИЛКА] Урок [{lesson.Id}]: {ex.Message}");
    }
    finally
    {
        Console.ResetColor();
    }
}

static void RunAllLessons(List<ILesson> lessons)
{
    int passed = 0;
    int failed = 0;
    foreach (var lesson in lessons)
    {
        Console.Write($"Running [{lesson.Id}] {lesson.Title}... ");
        try
        {
            lesson.Run();
            passed++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            failed++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAIL: {ex.Message}");
        }
        finally
        {
            Console.ResetColor();
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Підсумок: Успішно = {passed}, Помилок = {failed}, Усього = {lessons.Count}");
}

static void Pause()
{
    Console.WriteLine("\nНатисніть Enter для продовження...");
    Console.ReadLine();
}

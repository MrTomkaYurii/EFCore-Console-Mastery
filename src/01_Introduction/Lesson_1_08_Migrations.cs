using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01;

/// <summary>
/// Урок 1.8. Управління схемою БД та міграції.
/// Демонструє наскрізний MasteryDbContext (схема dbo з 8 зв'язаними таблицями),
/// інспекцію міграцій та генерацію T-SQL скрипта створення схеми.
/// </summary>
public class Lesson_1_08_Migrations : ILesson
{
    public string Id => "1.8";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Управління схемою БД та міграції";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.2.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 8;

    public void Run()
    {
        SqlServerDbHelper.EnsureDatabaseExists();

        SqlServerDbHelper.PrintSection("1. Інспекція міграцій наскрізного MasteryDbContext (схема dbo)");
        using (var db = new MasteryDbContext())
        {
            var allMigrations = db.Database.GetMigrations().ToList();
            var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
            var pendingMigrations = db.Database.GetPendingMigrations().ToList();

            Console.WriteLine($"Усього міграцій у збірці: {allMigrations.Count}");
            Console.WriteLine($"Застосовано в БД:         {appliedMigrations.Count}");
            Console.WriteLine($"Очікують застосування:    {pendingMigrations.Count}");
            Console.WriteLine("Команда для створення міграції:");
            Console.WriteLine("  dotnet ef migrations add InitialCreate --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp");
        }

        SqlServerDbHelper.PrintSection("2. Генерація T-SQL скрипта повної схеми dbo (GenerateCreateScript)");
        using (var db = new MasteryDbContext())
        {
            var script = db.Database.GenerateCreateScript();
            var previewLines = script.Split('\n').Take(22);
            Console.WriteLine(string.Join('\n', previewLines));
            Console.WriteLine("... (повний скрипт містить 8 зв'язаних таблиць: Countries, Companies, Roles, Users, UserProfiles, Products, Courses, Students)");
        }

        SqlServerDbHelper.PrintSection("3. Ініціалізація та читання наскрізної БД (MasteryDbContext.ResetAndSeed)");
        MasteryDbContext.ResetAndSeed();
        using (var db = new MasteryDbContext())
        {
            var users = db.Users
                .Include(u => u.Company)!.ThenInclude(c => c!.Country)
                .Include(u => u.Role)
                .AsNoTracking()
                .ToList();

            foreach (var u in users)
                Console.WriteLine($"  {u.Id}. {u.Name} ({u.Age}) — {u.Role?.Name} @ {u.Company?.Name} ({u.Company?.Country?.Name})");
        }
    }
}

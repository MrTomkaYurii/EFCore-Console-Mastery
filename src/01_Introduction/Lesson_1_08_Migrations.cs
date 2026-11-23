using EFCoreConsoleMastery.Core;
using EFCoreConsoleMastery.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Topics.Ch01;

/// <summary>
/// Урок 1.8. Управління схемою БД та міграції (Code-First покрокова демонстрація).
/// Файл структуровано як живий сценарій для лектора: Кроки 1–6 із готовими командами .NET CLI та PMC у коментарях.
/// </summary>
public class Lesson_1_08_Migrations : ILesson
{
    public string Id => "1.8";
    public string Chapter => "01. Вступ до Entity Framework Core";
    public string Title => "Управління схемою БД та міграції";
    public string MetanitUrl => "https://metanit.com/sharp/efcore/2.2.php";
    public int ChapterOrder => 1;
    public int LessonOrder => 8;

    // =========================================================================
    // КРОК 1. ОПИСУЄМО МОДЕЛІ (Code First)
    // =========================================================================
    // У наскрізному проєкті моделі вже винесені в EFCoreMastery.Core/Domain/MasteryDbContext.cs:
    //   Country -> Company -> User -> UserProfile (1:1)
    //                      -> Product
    //   Role    -> User
    //   Course <-> Student (M:N)
    //
    // Для живого демо на парі можна додати нову властивість у клас User (наприклад, public string? Email { get; set; })
    // після першої міграції — щоб показати, як EF Core автоматично згенерує ALTER TABLE у другій міграції.

    // =========================================================================
    // КРОК 2. НАЛАШТОВУЄМО DbContext ТА IDesignTimeDbContextFactory
    // =========================================================================
    // У MasteryDbContext.cs визначено:
    //   - MasteryDbContext        — сам контекст із DbSet<T> та OnModelCreating()
    //   - MasteryDbContextFactory — фабрика IDesignTimeDbContextFactory<MasteryDbContext>,
    //                               яку автоматично знаходять інструменти dotnet ef / PMC.
    //
    // Обов'язкові NuGet-пакети (вже встановлені в EFCoreMastery.Core):
    //   dotnet add package Microsoft.EntityFrameworkCore.SqlServer
    //   dotnet add package Microsoft.EntityFrameworkCore.Design
    //
    // Глобальний CLI-інструмент (ставиться 1 раз на ПК):
    //   dotnet tool install --global dotnet-ef

    // =========================================================================
    // КРОК 3. СТВОРЮЄМО ПЕРШУ МІГРАЦІЮ (InitialCreate)
    // =========================================================================
    // .NET CLI (виконувати з кореня репозиторію):
    //   dotnet ef migrations add InitialCreate --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp
    //
    // Visual Studio (Package Manager Console -> Default project: EFCoreMastery.Core):
    //   Add-Migration InitialCreate
    //
    // Що з'явиться в src/EFCoreMastery.Core/Migrations/:
    //   1) <Timestamp>_InitialCreate.cs          — методи Up() (застосування) та Down() (відкат)
    //   2) <Timestamp>_InitialCreate.Designer.cs — метадані моделі на момент цієї міграції
    //   3) MasteryDbContextModelSnapshot.cs      — поточний «знімок» моделі для обчислення наступних змін

    // =========================================================================
    // КРОК 4. ВИКОЧУЄМО МІГРАЦІЮ В БАЗУ ДАНИХ (MS SQL Server)
    // =========================================================================
    // .NET CLI:
    //   dotnet ef database update --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp
    //
    // Visual Studio (PMC):
    //   Update-Database
    //
    // Результат у MS SQL Server (EFCoreMasteryDb):
    //   - Створюються всі таблиці схеми dbo
    //   - Створюється службова таблиця dbo.__EFMigrationsHistory із записом про InitialCreate

    // =========================================================================
    // КРОК 5. ЕВОЛЮЦІЯ СХЕМИ (2-га міграція, відкат та видалення)
    // =========================================================================
    // 5.1. Додаємо нове поле в модель (наприклад, User.Email) і створюємо нову міграцію:
    //   dotnet ef migrations add AddUserEmail --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp
    //   dotnet ef database update --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp
    //
    // 5.2. Відкат до попередньої міграції (виконає метод Down() для AddUserEmail):
    //   dotnet ef database update InitialCreate --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp
    //   (PMC: Update-Database InitialCreate)
    //
    // 5.3. Видалити останню НЕЗАСТОСОВАНУ міграцію (ніколи не видаляйте файли вручну!):
    //   dotnet ef migrations remove --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp
    //   (PMC: Remove-Migration)

    // =========================================================================
    // КРОК 6. ДЕПЛОЙ У ПРОДАКШЕН (SQL-скрипт або виконуваний бандл)
    // =========================================================================
    // Ідемпотентний T-SQL скрипт (безпечно запускати повторно на будь-якому середовищі):
    //   dotnet ef migrations script --idempotent -o migrate.sql --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp
    //   (PMC: Script-Migration -Idempotent)
    //
    // Самодостатній виконуваний бандл (не потребує .NET SDK на сервері):
    //   dotnet ef migrations bundle --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp
    //   (PMC: Bundle-Migration)

    public void Run()
    {
        SqlServerDbHelper.EnsureDatabaseExists();

        SqlServerDbHelper.PrintSection("Кроки 1-2. Модель та DbContext (наскрізний MasteryDbContext, схема dbo)");
        Console.WriteLine("  Таблиці моделі: Countries, Companies, Roles, Users, UserProfiles, Products, Courses, Students");
        Console.WriteLine("  Фабрика CLI:    MasteryDbContextFactory (IDesignTimeDbContextFactory<MasteryDbContext>)");

        SqlServerDbHelper.PrintSection("Кроки 3-4. Інспекція міграцій у поточній збірці та БД");
        using (var db = new MasteryDbContext())
        {
            var allMigrations = db.Database.GetMigrations().ToList();
            var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
            var pendingMigrations = db.Database.GetPendingMigrations().ToList();

            Console.WriteLine($"  Усього міграцій у збірці: {allMigrations.Count}");
            Console.WriteLine($"  Застосовано в БД:         {appliedMigrations.Count}");
            Console.WriteLine($"  Очікують застосування:    {pendingMigrations.Count}");

            if (allMigrations.Count == 0)
            {
                Console.WriteLine("  Підказка: створіть першу міграцію командою:");
                Console.WriteLine("    dotnet ef migrations add InitialCreate --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp");
                Console.WriteLine("  А потім застосуйте її:");
                Console.WriteLine("    dotnet ef database update --project src/EFCoreMastery.Core --startup-project src/EFCoreMastery.ConsoleApp");
            }
            else if (pendingMigrations.Count > 0)
            {
                Console.WriteLine("  Застосовуємо очікуючі міграції програмно через db.Database.Migrate()...");
                db.Database.Migrate();
            }
        }

        SqlServerDbHelper.PrintSection("Крок 5. Прев'ю T-SQL скрипта створення схеми (GenerateCreateScript)");
        using (var db = new MasteryDbContext())
        {
            var script = db.Database.GenerateCreateScript();
            var previewLines = script.Split('\n').Take(20);
            Console.WriteLine(string.Join('\n', previewLines));
            Console.WriteLine("... (повний скрипт створює всі 8 зв'язаних таблиць схеми dbo)");
        }

        SqlServerDbHelper.PrintSection("Крок 6. Наповнення та перевірка даних у наскрізній БД (dbo)");
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

using Microsoft.EntityFrameworkCore;

namespace EFCoreConsoleMastery.Core;

/// <summary>
/// Допоміжний клас для створення ізольованих SQLite баз даних для кожного уроку.
/// Усі файли БД зберігаються в папці Databases/ і перестворюються перед запуском прикладу.
/// </summary>
public static class SqliteDbHelper
{
    private static readonly string DbDirectory = Path.Combine(AppContext.BaseDirectory, "Databases");

    /// <summary>
    /// Повертає рядок підключення до ізольованого файлу SQLite для конкретного уроку.
    /// </summary>
    /// <param name="dbName">Ім'я файлу бази даних (без розширення .db).</param>
    public static string GetConnectionString(string dbName)
    {
        Directory.CreateDirectory(DbDirectory);
        var dbPath = Path.Combine(DbDirectory, $"{dbName}.db");
        return $"Data Source={dbPath}";
    }

    /// <summary>
    /// Повертає повний шлях до файлу SQLite бази даних.
    /// </summary>
    public static string GetDbPath(string dbName)
    {
        Directory.CreateDirectory(DbDirectory);
        return Path.Combine(DbDirectory, $"{dbName}.db");
    }

    /// <summary>
    /// Гарантує чисту базу даних для демонстрації: видаляє стару (якщо існує) та створює нову схему.
    /// </summary>
    public static void RecreateDatabase(DbContext context)
    {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Друкує заголовок секції в консолі для наочного структурування виводу.
    /// </summary>
    public static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== {title} ===");
        Console.ResetColor();
    }
}

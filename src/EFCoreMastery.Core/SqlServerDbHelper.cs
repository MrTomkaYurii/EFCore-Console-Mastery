using EFCoreConsoleMastery.Core.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EFCoreConsoleMastery.Core;

/// <summary>
/// Помічник для підключення до MS SQL Server із appsettings.json та підготовки чистої схеми уроку.
/// </summary>
public static class SqlServerDbHelper
{
    // Читаємо налаштування з appsettings.json (та локального appsettings.Development.json, якщо він є)
    private static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build();

    /// <summary>Активний профіль із appsettings.json ("SqlServerDocker" або "SqlServerLocal").</summary>
    public static string ActiveConnectionName => Config["ActiveSqlServer"] ?? "SqlServerDocker";

    /// <summary>Активний рядок підключення до MS SQL Server.</summary>
    public static string ConnectionString => GetConnectionString(ActiveConnectionName);

    /// <summary>Повертає рядок підключення за іменем із appsettings.json.</summary>
    public static string GetConnectionString(string name) =>
        Config.GetConnectionString(name)
        ?? "Server=localhost,1434;Database=EFCoreMasteryDb;User Id=sa;Password=StrongPass123!;TrustServerCertificate=True;Encrypt=False;";

    /// <summary>Створює БД EFCoreMasteryDb на сервері, якщо її ще немає.</summary>
    public static void EnsureDatabaseExists(string? connectionString = null) =>
        SchemaResetter.EnsureDatabaseExists(connectionString ?? ConnectionString, useCache: connectionString is null);

    /// <summary>Перестворює лише схему поточного уроку (наприклад, "l3_01") без впливу на інші схеми.</summary>
    public static void RecreateSchema(DbContext context, string schemaName)
    {
        var connStr = context.Database.GetConnectionString() ?? ConnectionString;
        EnsureDatabaseExists(connStr);
        SchemaResetter.DropAllObjects(connStr, schemaName);
        SchemaResetter.ExecuteScript(connStr, context.Database.GenerateCreateScript());
    }

    /// <summary>Очищує всі об'єкти у вказаній SQL-схемі.</summary>
    public static void DropSchemaObjects(string connectionString, string schemaName)
    {
        EnsureDatabaseExists(connectionString);
        SchemaResetter.DropAllObjects(connectionString, schemaName);
    }

    /// <summary>Друкує заголовок секції в консолі.</summary>
    public static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== {title} ===");
        Console.ResetColor();
    }
}

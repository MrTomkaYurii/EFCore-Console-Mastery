using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EFCoreConsoleMastery.Core;

/// <summary>
/// Допоміжний клас для підключення до MS SQL Server (Docker або LocalDB) через appsettings.json
/// та ізольованого перестворення SQL-схем усередині спільної БД EFCoreMasteryDb.
/// </summary>
public static class SqlServerDbHelper
{
    private static readonly Lazy<IConfigurationRoot> Config = new(() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build());

    private static bool _databaseChecked;

    /// <summary>
    /// Ім'я активного рядка підключення з appsettings.json ("SqlServerDocker" або "SqlServerLocal").
    /// </summary>
    public static string ActiveConnectionName =>
        Config.Value["ActiveSqlServer"] ?? "SqlServerDocker";

    /// <summary>
    /// Активний рядок підключення до MS SQL Server (за замовчуванням SqlServerDocker).
    /// </summary>
    public static string ConnectionString => GetConnectionString(ActiveConnectionName);

    /// <summary>
    /// Повертає рядок підключення за ім'ям із секції ConnectionStrings у appsettings.json.
    /// </summary>
    public static string GetConnectionString(string name)
    {
        var connStr = Config.Value.GetConnectionString(name);
        if (!string.IsNullOrWhiteSpace(connStr))
            return connStr;

        return name switch
        {
            "SqlServerLocal" => @"Server=(localdb)\mssqllocaldb;Database=EFCoreMasteryDb;Trusted_Connection=True;TrustServerCertificate=True;",
            _ => "Server=localhost,1434;Database=EFCoreMasteryDb;User Id=sa;Password=StrongPass123!;TrustServerCertificate=True;Encrypt=False;"
        };
    }

    /// <summary>
    /// Гарантує, що спільна база даних EFCoreMasteryDb існує на сервері MS SQL Server.
    /// </summary>
    public static void EnsureDatabaseExists(string? connectionString = null)
    {
        if (_databaseChecked && connectionString is null)
            return;

        var targetBuilder = new SqlConnectionStringBuilder(connectionString ?? ConnectionString);
        var dbName = string.IsNullOrWhiteSpace(targetBuilder.InitialCatalog)
            ? "EFCoreMasteryDb"
            : targetBuilder.InitialCatalog;

        var masterBuilder = new SqlConnectionStringBuilder(targetBuilder.ConnectionString)
        {
            InitialCatalog = "master"
        };

        using var conn = new SqlConnection(masterBuilder.ConnectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            IF DB_ID(N'{dbName.Replace("'", "''")}') IS NULL
                CREATE DATABASE [{dbName.Replace("]", "]]")}];
            """;
        cmd.ExecuteNonQuery();

        if (connectionString is null)
            _databaseChecked = true;
    }

    /// <summary>
    /// Чисто перестворює ТІЛЬКИ вказану SQL-схему (наприклад, "l3_01" або "dbo")
    /// всередині спільної БД EFCoreMasteryDb, не зачіпаючи інші схеми.
    /// </summary>
    public static void RecreateSchema(DbContext context, string schemaName)
    {
        var connStr = context.Database.GetConnectionString() ?? ConnectionString;
        EnsureDatabaseExists(connStr);

        DropSchemaObjects(connStr, schemaName);

        var script = context.Database.GenerateCreateScript();
        ExecuteSqlBatches(connStr, script);
    }

    /// <summary>
    /// Видаляє всі об'єкти (FK, темпоральні таблиці, таблиці, views, процедури, функції, sequences)
    /// всередині заданої SQL-схеми та перестворює порожню схему (окрім системної dbo).
    /// </summary>
    public static void DropSchemaObjects(string connectionString, string schemaName)
    {
        EnsureDatabaseExists(connectionString);

        var safeSchema = schemaName.Replace("'", "''");
        var cleanupSql = $"""
            DECLARE @schema NVARCHAR(128) = N'{safeSchema}';
            DECLARE @sql NVARCHAR(MAX) = N'';

            IF SCHEMA_ID(@schema) IS NOT NULL
            BEGIN
                -- 1. Вимкнути SYSTEM_VERSIONING для Temporal Tables у цій схемі
                SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' SET (SYSTEM_VERSIONING = OFF);' + CHAR(10)
                FROM sys.tables t
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE s.name = @schema AND t.temporal_type = 2;
                IF LEN(@sql) > 0 EXEC sp_executesql @sql;

                -- 2. Видалити всі зовнішні ключі, що посилаються на/з цієї схеми
                SET @sql = N'';
                SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
                FROM sys.foreign_keys fk
                WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = @schema
                   OR OBJECT_SCHEMA_NAME(fk.referenced_object_id) = @schema;
                IF LEN(@sql) > 0 EXEC sp_executesql @sql;

                -- 3. Видалити Views, Stored Procedures, Functions, Sequences, Tables у цій схемі
                SET @sql = N'';
                SELECT @sql += N'DROP VIEW ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' + CHAR(10)
                FROM sys.views WHERE schema_id = SCHEMA_ID(@schema);
                SELECT @sql += N'DROP PROCEDURE ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' + CHAR(10)
                FROM sys.procedures WHERE schema_id = SCHEMA_ID(@schema);
                SELECT @sql += N'DROP FUNCTION ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' + CHAR(10)
                FROM sys.objects WHERE schema_id = SCHEMA_ID(@schema) AND type IN ('FN', 'IF', 'TF');
                SELECT @sql += N'DROP SEQUENCE ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' + CHAR(10)
                FROM sys.sequences WHERE schema_id = SCHEMA_ID(@schema);
                SELECT @sql += N'DROP TABLE ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' + CHAR(10)
                FROM sys.tables WHERE schema_id = SCHEMA_ID(@schema);
                IF LEN(@sql) > 0 EXEC sp_executesql @sql;
            END
            ELSE IF @schema <> N'dbo'
            BEGIN
                SET @sql = N'CREATE SCHEMA ' + QUOTENAME(@schema) + N';';
                EXEC sp_executesql @sql;
            END
            """;

        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = cleanupSql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Виконує T-SQL скрипт, розбитий на пакети за роздільником GO.
    /// </summary>
    public static void ExecuteSqlBatches(string connectionString, string sqlScript)
    {
        var batches = Regex.Split(sqlScript, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        using var conn = new SqlConnection(connectionString);
        conn.Open();

        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = trimmed;
            cmd.ExecuteNonQuery();
        }
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

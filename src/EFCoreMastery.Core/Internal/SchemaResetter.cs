using Microsoft.Data.SqlClient;

namespace EFCoreConsoleMastery.Core.Internal;

/// <summary>
/// Внутрішня службова інфраструктура для створення БД EFCoreMasteryDb та очищення окремих SQL-схем.
/// Студентам розбирати цей файл не обов'язково — достатньо користуватися простими методами SqlServerDbHelper.
/// </summary>
internal static class SchemaResetter
{
    private static bool _databaseChecked;

    public static void EnsureDatabaseExists(string connectionString, bool useCache)
    {
        if (useCache && _databaseChecked)
            return;

        var target = new SqlConnectionStringBuilder(connectionString);
        var dbName = string.IsNullOrWhiteSpace(target.InitialCatalog) ? "EFCoreMasteryDb" : target.InitialCatalog;
        var master = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };

        using var conn = new SqlConnection(master.ConnectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"IF DB_ID(@dbName) IS NULL CREATE DATABASE [{EscapeIdentifier(dbName)}];";
        cmd.Parameters.AddWithValue("@dbName", dbName);
        cmd.ExecuteNonQuery();

        if (useCache)
            _databaseChecked = true;
    }

    public static void DropAllObjects(string connectionString, string schemaName)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        if (!SchemaExists(connection, schemaName))
        {
            if (!schemaName.Equals("dbo", StringComparison.OrdinalIgnoreCase))
                ExecuteNonQuery(connection, $"CREATE SCHEMA [{EscapeIdentifier(schemaName)}];");
            return;
        }

        // 1. Вимкнути SYSTEM_VERSIONING у темпоральних таблицях
        RunGeneratedDropCommands(connection, schemaName, """
            SELECT N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' SET (SYSTEM_VERSIONING = OFF);'
            FROM sys.tables t
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = @schema AND t.temporal_type = 2
            """);

        // 2. Видалити зовнішні ключі, пов'язані з цією схемою
        RunGeneratedDropCommands(connection, schemaName, """
            SELECT N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' +
                   QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
            FROM sys.foreign_keys
            WHERE OBJECT_SCHEMA_NAME(parent_object_id) = @schema
               OR OBJECT_SCHEMA_NAME(referenced_object_id) = @schema
            """);

        // 3. Видалити views, процедури, функції, sequences та таблиці в цій схемі
        RunGeneratedDropCommands(connection, schemaName, """
            SELECT N'DROP VIEW ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' FROM sys.views WHERE schema_id = SCHEMA_ID(@schema)
            UNION ALL
            SELECT N'DROP PROCEDURE ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' FROM sys.procedures WHERE schema_id = SCHEMA_ID(@schema)
            UNION ALL
            SELECT N'DROP FUNCTION ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' FROM sys.objects WHERE schema_id = SCHEMA_ID(@schema) AND type IN ('FN', 'IF', 'TF')
            UNION ALL
            SELECT N'DROP SEQUENCE ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' FROM sys.sequences WHERE schema_id = SCHEMA_ID(@schema)
            UNION ALL
            SELECT N'DROP TABLE ' + QUOTENAME(@schema) + N'.' + QUOTENAME(name) + N';' FROM sys.tables WHERE schema_id = SCHEMA_ID(@schema)
            """);
    }

    public static void ExecuteScript(string connectionString, string sqlScript)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        var currentBatch = new List<string>();
        foreach (var rawLine in sqlScript.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                FlushBatch(connection, currentBatch);
            else
                currentBatch.Add(line);
        }

        FlushBatch(connection, currentBatch);
    }

    private static bool SchemaExists(SqlConnection connection, string schemaName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT CASE WHEN SCHEMA_ID(@schema) IS NOT NULL THEN 1 ELSE 0 END;";
        cmd.Parameters.AddWithValue("@schema", schemaName);
        return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
    }

    private static void RunGeneratedDropCommands(SqlConnection connection, string schemaName, string querySql)
    {
        var commands = new List<string>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = querySql;
            cmd.Parameters.AddWithValue("@schema", schemaName);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                commands.Add(reader.GetString(0));
        }

        foreach (var dropSql in commands)
            ExecuteNonQuery(connection, dropSql);
    }

    private static void FlushBatch(SqlConnection connection, List<string> lines)
    {
        var sql = string.Join(Environment.NewLine, lines).Trim();
        lines.Clear();
        if (!string.IsNullOrEmpty(sql))
            ExecuteNonQuery(connection, sql);
    }

    private static void ExecuteNonQuery(SqlConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string EscapeIdentifier(string identifier) => identifier.Replace("]", "]]");
}

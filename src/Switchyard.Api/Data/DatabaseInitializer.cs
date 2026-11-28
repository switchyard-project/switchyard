using Microsoft.Data.Sqlite;

namespace Switchyard.Api.Data;

public static class DatabaseInitializer
{
    public static void Initialize(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        // ADD COLUMN with a non-null REFERENCES default requires FK checks disabled.
        // This connection is only used at startup; repositories always enable them.
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false,
            ForeignKeys = false
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL;";
        command.ExecuteNonQuery();
        using var transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS warehouses (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                rows INTEGER NOT NULL CHECK(rows BETWEEN 1 AND 50),
                columns INTEGER NOT NULL CHECK(columns BETWEEN 1 AND 50)
            );
            INSERT INTO warehouses (id, name, rows, columns)
                VALUES (1, 'Almacén de demostración', 12, 12) ON CONFLICT(id) DO NOTHING;
            CREATE TABLE IF NOT EXISTS robots (id TEXT PRIMARY KEY NOT NULL, registered_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS transport_tasks (
                id INTEGER PRIMARY KEY, pickup TEXT NOT NULL, destination TEXT NOT NULL,
                robot_id TEXT REFERENCES robots(id), completed INTEGER NOT NULL DEFAULT 0
                    CHECK(completed = 0 OR (completed = 1 AND robot_id IS NOT NULL))
            );
            CREATE UNIQUE INDEX IF NOT EXISTS transport_tasks_active_robot
                ON transport_tasks(robot_id) WHERE completed = 0;
            CREATE TABLE IF NOT EXISTS warehouse_zones (
                id INTEGER PRIMARY KEY, warehouse_id INTEGER NOT NULL REFERENCES warehouses(id),
                name TEXT NOT NULL, kind TEXT NOT NULL CHECK(kind IN ('shelf', 'dock', 'charging')),
                start_row INTEGER NOT NULL CHECK(start_row BETWEEN 1 AND 50),
                start_column INTEGER NOT NULL CHECK(start_column BETWEEN 1 AND 50),
                rows INTEGER NOT NULL CHECK(rows BETWEEN 1 AND 50),
                columns INTEGER NOT NULL CHECK(columns BETWEEN 1 AND 50),
                UNIQUE(warehouse_id, name)
            );
            """;
        command.ExecuteNonQuery();
        AddWarehouseColumn(command, "robots");
        AddWarehouseColumn(command, "transport_tasks");
        command.CommandText = "PRAGMA foreign_key_check;";
        using (var reader = command.ExecuteReader())
        {
            if (reader.Read()) throw new InvalidOperationException("La base contiene referencias inválidas; no se aplicó la migración.");
        }
        transaction.Commit();
    }

    private static void AddWarehouseColumn(SqliteCommand command, string table)
    {
        // Table names come only from the two constants above, never from a request.
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = 'warehouse_id';";
        if ((long)command.ExecuteScalar()! != 0) return;
        command.CommandText = $"ALTER TABLE {table} ADD COLUMN warehouse_id INTEGER NOT NULL DEFAULT 1 REFERENCES warehouses(id);";
        command.ExecuteNonQuery();
    }
}

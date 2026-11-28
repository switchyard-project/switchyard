using Microsoft.Data.Sqlite;
using Switchyard.Contracts;

namespace Switchyard.Api.Data;

public sealed class WarehouseRepository
{
    private readonly string _connectionString;

    public WarehouseRepository(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = 5
        }.ToString();
    }

    public WarehouseResponse? Create(CreateWarehouseRequest request)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO warehouses (name, rows, columns) VALUES ($name, $rows, $columns)
            ON CONFLICT(name) DO NOTHING RETURNING id;
            """;
        command.Parameters.AddWithValue("$name", request.Name.Trim());
        command.Parameters.AddWithValue("$rows", request.Rows);
        command.Parameters.AddWithValue("$columns", request.Columns);
        var id = command.ExecuteScalar();
        return id is long value ? new WarehouseResponse(value, request.Name.Trim(), request.Rows, request.Columns, 0) : null;
    }

    public IReadOnlyList<WarehouseResponse> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT w.id, w.name, w.rows, w.columns, COUNT(r.id)
            FROM warehouses w LEFT JOIN robots r ON r.warehouse_id = w.id
            GROUP BY w.id ORDER BY w.id;
            """;
        using var reader = command.ExecuteReader();
        var warehouses = new List<WarehouseResponse>();
        while (reader.Read()) warehouses.Add(ReadWarehouse(reader));
        return warehouses;
    }

    public WarehouseResponse? GetById(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT w.id, w.name, w.rows, w.columns, COUNT(r.id)
            FROM warehouses w LEFT JOIN robots r ON r.warehouse_id = w.id
            WHERE w.id = $id GROUP BY w.id;
            """;
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadWarehouse(reader) : null;
    }

    public IReadOnlyList<ZoneResponse> GetZones(long warehouseId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, kind, start_row, start_column, rows, columns
            FROM warehouse_zones WHERE warehouse_id = $warehouseId ORDER BY id;
            """;
        command.Parameters.AddWithValue("$warehouseId", warehouseId);
        using var reader = command.ExecuteReader();
        var zones = new List<ZoneResponse>();
        while (reader.Read()) zones.Add(new ZoneResponse(reader.GetInt64(0), reader.GetString(1),
            reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6)));
        return zones;
    }

    public ZoneResponse? CreateZone(long warehouseId, CreateZoneRequest request)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        // Checking overlap and inserting share one write, so concurrent requests cannot both win.
        command.CommandText = """
            INSERT INTO warehouse_zones (warehouse_id, name, kind, start_row, start_column, rows, columns)
            SELECT $warehouseId, $name, $kind, $startRow, $startColumn, $rows, $columns
            FROM warehouses w WHERE w.id = $warehouseId
                AND $startRow + $rows - 1 <= w.rows AND $startColumn + $columns - 1 <= w.columns
                AND NOT EXISTS (
                    SELECT 1 FROM warehouse_zones z WHERE z.warehouse_id = w.id
                    AND z.start_row < $startRow + $rows AND z.start_row + z.rows > $startRow
                    AND z.start_column < $startColumn + $columns AND z.start_column + z.columns > $startColumn
                )
            ON CONFLICT(warehouse_id, name) DO NOTHING RETURNING id;
            """;
        command.Parameters.AddWithValue("$warehouseId", warehouseId);
        command.Parameters.AddWithValue("$name", request.Name.Trim());
        command.Parameters.AddWithValue("$kind", request.Kind);
        command.Parameters.AddWithValue("$startRow", request.StartRow);
        command.Parameters.AddWithValue("$startColumn", request.StartColumn);
        command.Parameters.AddWithValue("$rows", request.Rows);
        command.Parameters.AddWithValue("$columns", request.Columns);
        var id = command.ExecuteScalar();
        return id is long value ? new ZoneResponse(value, request.Name.Trim(), request.Kind,
            request.StartRow, request.StartColumn, request.Rows, request.Columns) : null;
    }

    private static WarehouseResponse ReadWarehouse(SqliteDataReader reader) =>
        new(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4));
}

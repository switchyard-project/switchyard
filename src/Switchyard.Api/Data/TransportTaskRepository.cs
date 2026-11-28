using Microsoft.Data.Sqlite;
using Switchyard.Contracts;

namespace Switchyard.Api.Data;

public sealed class TransportTaskRepository
{
    private readonly string _connectionString;

    public TransportTaskRepository(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false,
            DefaultTimeout = 5,
            ForeignKeys = true
        }.ToString();
    }

    public TransportTaskResponse Create(CreateTransportTaskRequest request)
    {
        var pickup = request.Pickup.Trim();
        var destination = request.Destination.Trim();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO transport_tasks (pickup, destination, warehouse_id)
            VALUES ($pickup, $destination, $warehouseId)
            RETURNING id;
            """;
        command.Parameters.AddWithValue("$warehouseId", request.WarehouseId);
        command.Parameters.AddWithValue("$pickup", pickup);
        command.Parameters.AddWithValue("$destination", destination);

        var id = (long)command.ExecuteScalar()!;
        return new TransportTaskResponse(id, pickup, destination, TransportTaskResponse.Pending, null, request.WarehouseId);
    }

    public TransportTaskResponse? GetById(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, pickup, destination, robot_id, completed, warehouse_id
            FROM transport_tasks
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadTask(reader) : null;
    }

    public IReadOnlyList<TransportTaskResponse> GetAll(long warehouseId = 1)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, pickup, destination, robot_id, completed, warehouse_id
            FROM transport_tasks
            WHERE warehouse_id = $warehouseId
            ORDER BY id;
            """;

        command.Parameters.AddWithValue("$warehouseId", warehouseId);
        using var reader = command.ExecuteReader();
        var tasks = new List<TransportTaskResponse>();
        while (reader.Read())
        {
            tasks.Add(ReadTask(reader));
        }

        return tasks;
    }

    public TransportTaskResponse? Claim(string robotId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        // Elegir y asignar en una misma escritura para que dos peticiones no elijan la misma tarea.
        command.CommandText = """
            UPDATE transport_tasks
            SET robot_id = $robotId
            WHERE id = (
                SELECT id FROM transport_tasks
                WHERE warehouse_id = (SELECT warehouse_id FROM robots WHERE id = $robotId)
                    AND completed = 0 AND (robot_id = $robotId OR robot_id IS NULL)
                ORDER BY robot_id IS NULL, id
                LIMIT 1
            )
            RETURNING id, pickup, destination, robot_id, completed, warehouse_id;
            """;
        command.Parameters.AddWithValue("$robotId", robotId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadTask(reader) : null;
    }

    public TransportTaskResponse? Complete(long id, string robotId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE transport_tasks
            SET completed = 1
            WHERE id = $id AND robot_id = $robotId
            RETURNING id, pickup, destination, robot_id, completed, warehouse_id;
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$robotId", robotId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadTask(reader) : null;
    }

    private static TransportTaskResponse ReadTask(SqliteDataReader reader)
    {
        var robotId = reader.IsDBNull(3) ? null : reader.GetString(3);
        var status = reader.GetBoolean(4)
            ? TransportTaskResponse.Completed
            : robotId is null ? TransportTaskResponse.Pending : TransportTaskResponse.Assigned;

        return new TransportTaskResponse(
            reader.GetInt64(0), reader.GetString(1), reader.GetString(2), status, robotId, reader.GetInt64(5));
    }
}

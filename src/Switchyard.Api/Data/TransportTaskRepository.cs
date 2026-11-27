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

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS transport_tasks (
                id INTEGER PRIMARY KEY,
                pickup TEXT NOT NULL,
                destination TEXT NOT NULL,
                robot_id TEXT REFERENCES robots(id),
                completed INTEGER NOT NULL DEFAULT 0
                    CHECK (completed = 0 OR (completed = 1 AND robot_id IS NOT NULL))
            );
            CREATE UNIQUE INDEX IF NOT EXISTS transport_tasks_active_robot
                ON transport_tasks (robot_id) WHERE completed = 0;
            """;
        command.ExecuteNonQuery();
    }

    public TransportTaskResponse Create(CreateTransportTaskRequest request)
    {
        var pickup = request.Pickup.Trim();
        var destination = request.Destination.Trim();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO transport_tasks (pickup, destination)
            VALUES ($pickup, $destination)
            RETURNING id;
            """;
        command.Parameters.AddWithValue("$pickup", pickup);
        command.Parameters.AddWithValue("$destination", destination);

        var id = (long)command.ExecuteScalar()!;
        return new TransportTaskResponse(id, pickup, destination, TransportTaskResponse.Pending, null);
    }

    public TransportTaskResponse? GetById(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, pickup, destination, robot_id, completed
            FROM transport_tasks
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadTask(reader) : null;
    }

    public IReadOnlyList<TransportTaskResponse> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, pickup, destination, robot_id, completed
            FROM transport_tasks
            ORDER BY id;
            """;

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
                WHERE completed = 0 AND (robot_id = $robotId OR robot_id IS NULL)
                ORDER BY robot_id IS NULL, id
                LIMIT 1
            )
            RETURNING id, pickup, destination, robot_id, completed;
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
            RETURNING id, pickup, destination, robot_id, completed;
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
            reader.GetInt64(0), reader.GetString(1), reader.GetString(2), status, robotId);
    }
}

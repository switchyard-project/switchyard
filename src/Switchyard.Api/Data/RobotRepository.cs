using System.Globalization;
using Microsoft.Data.Sqlite;
using Switchyard.Contracts;

namespace Switchyard.Api.Data;

public sealed class RobotRepository
{
    private readonly string _connectionString;

    public RobotRepository(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false,
            DefaultTimeout = 5,
            ForeignKeys = true
        }.ToString();
    }

    public bool TryAdd(RobotResponse robot)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO robots (id, registered_at, warehouse_id)
            VALUES ($id, $registeredAt, $warehouseId)
            ON CONFLICT(id) DO NOTHING;
            """;
        command.Parameters.AddWithValue("$id", robot.Id);
        command.Parameters.AddWithValue("$warehouseId", robot.WarehouseId);
        command.Parameters.AddWithValue("$registeredAt",
            robot.RegisteredAt.ToString("O", CultureInfo.InvariantCulture));

        return command.ExecuteNonQuery() == 1;
    }

    public RobotResponse? GetById(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, registered_at, warehouse_id FROM robots WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadRobot(reader) : null;
    }

    public IReadOnlyList<RobotResponse> GetAll(long warehouseId = 1)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, registered_at, warehouse_id FROM robots WHERE warehouse_id = $warehouseId ORDER BY id;";

        command.Parameters.AddWithValue("$warehouseId", warehouseId);
        using var reader = command.ExecuteReader();
        var robots = new List<RobotResponse>();
        while (reader.Read())
        {
            robots.Add(ReadRobot(reader));
        }

        return robots;
    }

    private static RobotResponse ReadRobot(SqliteDataReader reader)
    {
        return new RobotResponse(
            reader.GetString(0),
            DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture), reader.GetInt64(2));
    }
}

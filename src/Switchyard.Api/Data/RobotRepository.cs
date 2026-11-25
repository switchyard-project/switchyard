using System.Globalization;
using Microsoft.Data.Sqlite;
using Switchyard.Contracts;

namespace Switchyard.Api.Data;

public sealed class RobotRepository
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public RobotRepository(string databasePath)
    {
        _databasePath = databasePath;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false,
            DefaultTimeout = 5
        }.ToString();
    }

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS robots (
                id TEXT PRIMARY KEY NOT NULL,
                registered_at TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public bool TryAdd(RobotResponse robot)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO robots (id, registered_at)
            VALUES ($id, $registeredAt)
            ON CONFLICT(id) DO NOTHING;
            """;
        command.Parameters.AddWithValue("$id", robot.Id);
        command.Parameters.AddWithValue("$registeredAt",
            robot.RegisteredAt.ToString("O", CultureInfo.InvariantCulture));

        return command.ExecuteNonQuery() == 1;
    }

    public RobotResponse? GetById(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, registered_at FROM robots WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadRobot(reader) : null;
    }

    public IReadOnlyList<RobotResponse> GetAll()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, registered_at FROM robots ORDER BY id;";

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
            DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture));
    }
}

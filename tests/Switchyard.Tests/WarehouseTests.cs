using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Switchyard.Contracts;

namespace Switchyard.Tests;

public sealed class WarehouseTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public WarehouseTests()
    {
        _factory = new TestApiFactory(Path.Combine(_directory, "switchyard.db"));
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Warehouses_SeparateRobotsTasksAndClaims()
    {
        var warehouse = await CreateWarehouseAsync();
        await _client.PostAsJsonAsync("/api/robots", new { id = "R01" });
        await _client.PostAsJsonAsync("/api/robots", new { id = "R02", warehouseId = warehouse.Id });
        await _client.PostAsJsonAsync("/api/tasks", new { pickup = "A", destination = "B" });
        using var created = await _client.PostAsJsonAsync("/api/tasks", new { pickup = "C", destination = "D", warehouseId = warehouse.Id });
        var task = await created.Content.ReadFromJsonAsync<TransportTaskResponse>();
        var robots = await _client.GetFromJsonAsync<RobotResponse[]>($"/api/robots?warehouseId={warehouse.Id}");
        Assert.Equal("R02", Assert.Single(robots!).Id);
        Assert.Single((await _client.GetFromJsonAsync<TransportTaskResponse[]>($"/api/tasks?warehouseId={warehouse.Id}"))!);
        using var claim = await _client.PostAsync("/api/robots/R02/tasks/claim", null);
        Assert.Equal(task!.Id, (await claim.Content.ReadFromJsonAsync<TransportTaskResponse>())!.Id);
        Assert.Equal(1, (await _client.GetFromJsonAsync<WarehouseResponse>($"/api/warehouses/{warehouse.Id}"))!.RobotCount);
    }

    [Theory]
    [InlineData(0, 4, "A")]
    [InlineData(4, 51, "A")]
    [InlineData(4, 4, "  ")]
    public async Task CreateWarehouse_RejectsInvalidLayout(int rows, int columns, string name)
    {
        using var response = await _client.PostAsJsonAsync("/api/warehouses", new { name, rows, columns });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWarehouse_RejectsDuplicateTrimmedName()
    {
        await CreateWarehouseAsync();
        using var response = await _client.PostAsJsonAsync("/api/warehouses", new { name = " Norte ", rows = 4, columns = 4 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateZone_RejectsOverlapIncludingConcurrentRequestsButAllowsAdjacentZones()
    {
        var warehouse = await CreateWarehouseAsync();
        var path = $"/api/warehouses/{warehouse.Id}/zones";
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(i =>
            _client.PostAsJsonAsync(path, new { name = $"Zona {i}", kind = "shelf", startRow = 1, startColumn = 1, rows = 2, columns = 2 })));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in responses) response.Dispose();
        using var adjacent = await _client.PostAsJsonAsync(path, new { name = "Bahía", kind = "dock", startRow = 1, startColumn = 3, rows = 2, columns = 2 });
        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);
    }

    [Theory]
    [InlineData(4, 4, 2, 1, "shelf")]
    [InlineData(1, 1, 1, 1, "unknown")]
    [InlineData(0, 1, 1, 1, "dock")]
    public async Task CreateZone_RejectsInvalidBoundsOrKind(int startRow, int startColumn, int rows, int columns, string kind)
    {
        var warehouse = await CreateWarehouseAsync();
        using var response = await _client.PostAsJsonAsync($"/api/warehouses/{warehouse.Id}/zones", new { name = "Zona", kind, startRow, startColumn, rows, columns });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingWarehouse_RejectsDependentRecords()
    {
        using var robot = await _client.PostAsJsonAsync("/api/robots", new { id = "R01", warehouseId = 999 });
        using var task = await _client.PostAsJsonAsync("/api/tasks", new { pickup = "A", destination = "B", warehouseId = 999 });
        using var zones = await _client.GetAsync("/api/warehouses/999/zones");
        Assert.Equal(HttpStatusCode.NotFound, robot.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, task.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, zones.StatusCode);
    }

    [Fact]
    public async Task Layout_PersistsAfterRestart()
    {
        var warehouse = await CreateWarehouseAsync();
        using var response = await _client.PostAsJsonAsync($"/api/warehouses/{warehouse.Id}/zones", new { name = "Carga", kind = "charging", startRow = 2, startColumn = 2, rows = 1, columns = 1 });
        response.EnsureSuccessStatusCode();
        _client.Dispose();
        _factory.Dispose();
        using var restarted = new TestApiFactory(Path.Combine(_directory, "switchyard.db"));
        using var client = restarted.CreateClient();
        Assert.Equal(warehouse, await client.GetFromJsonAsync<WarehouseResponse>($"/api/warehouses/{warehouse.Id}"));
        Assert.Equal("Carga", Assert.Single((await client.GetFromJsonAsync<ZoneResponse[]>($"/api/warehouses/{warehouse.Id}/zones"))!).Name);
    }

    [Fact]
    public async Task LegacyDatabase_MigratesTwiceWithoutLosingAssignmentsOrCompletion()
    {
        var path = Path.Combine(_directory, "legacy.db");
        using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE robots (id TEXT PRIMARY KEY NOT NULL, registered_at TEXT NOT NULL);
                CREATE TABLE transport_tasks (id INTEGER PRIMARY KEY, pickup TEXT NOT NULL, destination TEXT NOT NULL,
                    robot_id TEXT REFERENCES robots(id), completed INTEGER NOT NULL DEFAULT 0);
                INSERT INTO robots VALUES ('OLD', '2026-11-27T12:00:00+00:00');
                INSERT INTO transport_tasks VALUES (1, 'A', 'B', 'OLD', 1), (2, 'C', 'D', 'OLD', 0);
                """;
            command.ExecuteNonQuery();
        }
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var factory = new TestApiFactory(path);
            using var client = factory.CreateClient();
            var robot = Assert.Single((await client.GetFromJsonAsync<RobotResponse[]>("/api/robots"))!);
            Assert.Equal(1, robot.WarehouseId);
            var tasks = await client.GetFromJsonAsync<TransportTaskResponse[]>("/api/tasks");
            Assert.Equal(new[] { "completed", "assigned" }, tasks!.Select(task => task.Status));
            Assert.All(tasks!, task => { Assert.Equal("OLD", task.RobotId); Assert.Equal(1, task.WarehouseId); });
        }
    }

    private async Task<WarehouseResponse> CreateWarehouseAsync()
    {
        using var response = await _client.PostAsJsonAsync("/api/warehouses", new { name = "Norte", rows = 4, columns = 4 });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WarehouseResponse>())!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}

using System.Net;
using System.Net.Http.Json;
using Switchyard.Contracts;
using Switchyard.Robot;

namespace Switchyard.Tests;

public sealed class TransportTaskTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;
    private readonly RobotApiClient _robot;

    public TransportTaskTests()
    {
        _factory = new TestApiFactory(Path.Combine(_directory, "switchyard.db"));
        _client = _factory.CreateClient();
        _robot = new RobotApiClient(_client);
    }

    [Fact]
    public async Task Create_ReturnsPendingTaskAndItsLocation()
    {
        using var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            pickup = " Estante A ",
            destination = "Muelle 1"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TransportTaskResponse>();
        Assert.NotNull(task);
        Assert.Equal("Estante A", task.Pickup);
        Assert.Equal("Muelle 1", task.Destination);
        Assert.Equal("pending", task.Status);
        Assert.Null(task.RobotId);
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(task, await _client.GetFromJsonAsync<TransportTaskResponse>(response.Headers.Location));
    }

    [Theory]
    [InlineData(null, "Muelle 1")]
    [InlineData("Estante A", " ")]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghi", "Muelle 1")]
    public async Task Create_WithInvalidPickupOrDestination_ReturnsBadRequest(string? pickup, string? destination)
    {
        using var response = await _client.PostAsJsonAsync("/api/tasks", new { pickup, destination });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await _robot.RegisterAsync("R01");
        Assert.Null(await _robot.ClaimTaskAsync("R01"));
    }

    [Fact]
    public async Task Claim_WithUnknownRobot_ReturnsNotFoundAndKeepsTaskPending()
    {
        var pending = await CreateTaskAsync();
        using var response = await _client.PostAsync("/api/robots/R01/tasks/claim", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(pending, await _client.GetFromJsonAsync<TransportTaskResponse>($"/api/tasks/{pending.Id}"));
    }

    [Fact]
    public async Task Claim_WhenTwoRobotsCompete_AssignsTheTaskOnlyOnce()
    {
        await _robot.RegisterAsync("R01");
        await _robot.RegisterAsync("R02");
        var pending = await CreateTaskAsync();

        var claims = await Task.WhenAll(
            Task.Run(() => _robot.ClaimTaskAsync("R01")),
            Task.Run(() => _robot.ClaimTaskAsync("R02")));

        var assigned = Assert.Single(claims.OfType<TransportTaskResponse>());
        Assert.Equal(pending.Id, assigned.Id);
        Assert.Equal(assigned, await _client.GetFromJsonAsync<TransportTaskResponse>($"/api/tasks/{pending.Id}"));
    }

    [Fact]
    public async Task Claim_WhenTheSameRobotRequestsTwice_ReturnsItsOldestActiveTask()
    {
        await _robot.RegisterAsync("R01");
        var first = await CreateTaskAsync();
        var second = await CreateTaskAsync();

        var claims = await Task.WhenAll(
            Task.Run(() => _robot.ClaimTaskAsync("R01")),
            Task.Run(() => _robot.ClaimTaskAsync("R01")));

        foreach (var task in claims)
        {
            Assert.NotNull(task);
            Assert.Equal(first.Id, task.Id);
        }

        await _robot.CompleteTaskAsync("R01", first.Id);
        var next = await _robot.ClaimTaskAsync("R01");
        Assert.NotNull(next);
        Assert.Equal(second.Id, next.Id);
    }

    [Fact]
    public async Task Complete_RequiresTheAssignedRobotAndAcceptsARepeat()
    {
        await _robot.RegisterAsync("R01");
        await _robot.RegisterAsync("R02");
        var pending = await CreateTaskAsync();
        using var unassigned = await _client.PutAsync($"/api/robots/R01/tasks/{pending.Id}/completion", null);
        Assert.Equal(HttpStatusCode.Conflict, unassigned.StatusCode);

        var assigned = await _robot.ClaimTaskAsync("R01");
        using var otherRobot = await _client.PutAsync($"/api/robots/R02/tasks/{pending.Id}/completion", null);
        Assert.Equal(HttpStatusCode.Conflict, otherRobot.StatusCode);
        Assert.Equal(assigned, await _client.GetFromJsonAsync<TransportTaskResponse>($"/api/tasks/{pending.Id}"));

        var completed = await _robot.CompleteTaskAsync("R01", pending.Id);
        Assert.Equal("completed", completed.Status);
        Assert.Equal("R01", completed.RobotId);
        Assert.Equal(completed, await _robot.CompleteTaskAsync("R01", pending.Id));

        using var missing = await _client.PutAsync($"/api/robots/R01/tasks/{pending.Id + 1}/completion", null);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Null(await _robot.ClaimTaskAsync("R01"));
    }

    [Fact]
    public async Task GetById_AfterServerRestart_PreservesAssignmentAndCompletion()
    {
        await _robot.RegisterAsync("R01");
        await _robot.RegisterAsync("R02");
        var first = await CreateTaskAsync();
        var second = await CreateTaskAsync();
        await _robot.ClaimTaskAsync("R01");
        var completed = await _robot.CompleteTaskAsync("R01", first.Id);
        var assigned = await _robot.ClaimTaskAsync("R02");
        Assert.NotNull(assigned);
        Assert.Equal(second.Id, assigned.Id);

        _client.Dispose();
        _factory.Dispose();

        using var restarted = new TestApiFactory(Path.Combine(_directory, "switchyard.db"));
        using var restartedClient = restarted.CreateClient();
        Assert.Equal(completed, await restartedClient.GetFromJsonAsync<TransportTaskResponse>($"/api/tasks/{first.Id}"));
        Assert.Equal(assigned, await new RobotApiClient(restartedClient).ClaimTaskAsync("R02"));
    }

    private async Task<TransportTaskResponse> CreateTaskAsync()
    {
        using var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            pickup = "Estante A",
            destination = "Muelle 1"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TransportTaskResponse>())!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}

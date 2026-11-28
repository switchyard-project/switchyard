using System.Net;
using System.Net.Http.Json;
using Switchyard.Contracts;
using Switchyard.Robot;

namespace Switchyard.Tests;

public sealed class TaskAssignmentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public TaskAssignmentTests()
    {
        _factory = new TestApiFactory(Path.Combine(_directory, "switchyard.db"));
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AssignedTask_IsIdempotentAndPickedUpByDotNetClientBeforeOlderPendingTask()
    {
        await _client.PostAsJsonAsync("/api/robots", new { id = "R01" });
        await CreateTaskAsync();
        var task = await CreateTaskAsync();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var response = await AssignAsync(task.Id, "R01");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        var robot = new RobotApiClient(_client);
        Assert.Equal(task.Id, (await robot.ClaimTaskAsync("R01"))!.Id);
        Assert.Equal("completed", (await robot.CompleteTaskAsync("R01", task.Id)).Status);
        using var completed = await AssignAsync(task.Id, "R01");
        Assert.Equal(HttpStatusCode.Conflict, completed.StatusCode);
    }

    [Fact]
    public async Task Assign_RejectsBusyRobotAndTaskOwnedByAnotherRobot()
    {
        await _client.PostAsJsonAsync("/api/robots", new { id = "R01" });
        await _client.PostAsJsonAsync("/api/robots", new { id = "R02" });
        var first = await CreateTaskAsync();
        var second = await CreateTaskAsync();
        using var assigned = await AssignAsync(first.Id, "R01");
        assigned.EnsureSuccessStatusCode();
        using var busy = await AssignAsync(second.Id, "R01");
        using var owned = await AssignAsync(first.Id, "R02");
        Assert.Equal(HttpStatusCode.Conflict, busy.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, owned.StatusCode);
    }

    [Fact]
    public async Task Assign_RejectsAnotherWarehouse()
    {
        using var created = await _client.PostAsJsonAsync("/api/warehouses", new { name = "Otro", rows = 3, columns = 3 });
        var warehouse = (await created.Content.ReadFromJsonAsync<WarehouseResponse>())!;
        await _client.PostAsJsonAsync("/api/robots", new { id = "R01", warehouseId = warehouse.Id });
        var task = await CreateTaskAsync();
        using var assigned = await AssignAsync(task.Id, "R01");
        Assert.Equal(HttpStatusCode.Conflict, assigned.StatusCode);
        using var claimed = await _client.PostAsync("/api/robots/R01/tasks/claim", null);
        Assert.Equal(HttpStatusCode.NoContent, claimed.StatusCode);
    }

    [Fact]
    public async Task Assign_RejectsUnknownTaskRobotAndInvalidInput()
    {
        var task = await CreateTaskAsync();
        using var unknownRobot = await AssignAsync(task.Id, "MISSING");
        await _client.PostAsJsonAsync("/api/robots", new { id = "R01" });
        using var unknownTask = await AssignAsync(999, "R01");
        using var invalid = await AssignAsync(task.Id, "bad id");
        Assert.Equal(HttpStatusCode.NotFound, unknownRobot.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownTask.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task ConcurrentManualAssignments_GiveOneRobotOnlyOneTask()
    {
        await _client.PostAsJsonAsync("/api/robots", new { id = "R01" });
        var tasks = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => CreateTaskAsync()));
        var responses = await Task.WhenAll(tasks.Select(task => AssignAsync(task.Id, "R01")));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in responses) response.Dispose();
    }

    [Fact]
    public async Task ConcurrentClaimAndManualAssignment_NeverGiveSameTaskToTwoRobots()
    {
        await _client.PostAsJsonAsync("/api/robots", new { id = "R01" });
        await _client.PostAsJsonAsync("/api/robots", new { id = "R02" });
        var task = await CreateTaskAsync();
        var responses = await Task.WhenAll(AssignAsync(task.Id, "R01"), _client.PostAsync("/api/robots/R02/tasks/claim", null));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Contains(responses, response => response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NoContent);
        var saved = (await _client.GetFromJsonAsync<TransportTaskResponse>($"/api/tasks/{task.Id}"))!;
        Assert.Equal("assigned", saved.Status);
        Assert.Contains(saved.RobotId, new[] { "R01", "R02" });
        foreach (var response in responses) response.Dispose();
    }

    private Task<HttpResponseMessage> AssignAsync(long id, string robotId) =>
        _client.PutAsJsonAsync($"/api/tasks/{id}/assignment", new { robotId });

    private async Task<TransportTaskResponse> CreateTaskAsync()
    {
        using var response = await _client.PostAsJsonAsync("/api/tasks", new { pickup = "A", destination = "B" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TransportTaskResponse>())!;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}

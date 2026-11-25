using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Switchyard.Contracts;
using Switchyard.Robot;

namespace Switchyard.Tests;

public sealed class RobotRegistrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "switchyard-tests", Guid.NewGuid().ToString("N"));
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public RobotRegistrationTests()
    {
        _factory = new TestApiFactory(Path.Combine(_directory, "robots.db"));
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WhenNoRobotsExist_ReturnsEmptyList()
    {
        var robots = await _client.GetFromJsonAsync<RobotResponse[]>("/api/robots");

        Assert.NotNull(robots);
        Assert.Empty(robots);
    }

    [Fact]
    public async Task Register_WithValidId_ReturnsCreatedRobotAndItsLocation()
    {
        var before = DateTimeOffset.UtcNow;
        using var response = await _client.PostAsJsonAsync("/api/robots", new { id = "R01" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith("/api/robots/R01", response.Headers.Location.ToString());

        var robot = await response.Content.ReadFromJsonAsync<RobotResponse>();
        Assert.NotNull(robot);
        Assert.Equal("R01", robot.Id);
        Assert.Equal(TimeSpan.Zero, robot.RegisteredAt.Offset);
        Assert.InRange(robot.RegisteredAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(robot, await _client.GetFromJsonAsync<RobotResponse>(response.Headers.Location));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("r01")]
    [InlineData("-R01")]
    [InlineData("R 01")]
    [InlineData("R01\n")]
    [InlineData("RÓBOT")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("R01'; DROP TABLE robots;--")]
    public async Task Register_WithInvalidId_ReturnsBadRequestWithoutSaving(string? id)
    {
        using var response = await _client.PostAsJsonAsync("/api/robots", new { id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.NotEmpty(problem.Errors);
        Assert.Empty((await _client.GetFromJsonAsync<RobotResponse[]>("/api/robots"))!);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"id\":123}")]
    [InlineData("{")]
    [InlineData("null")]
    public async Task Register_WithInvalidBody_ReturnsBadRequest(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/api/robots", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<RobotResponse[]>("/api/robots"))!);
    }

    [Theory]
    [InlineData("R")]
    [InlineData("0")]
    [InlineData("ROBOT-01_A")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task Register_WithValidBoundaryId_ReturnsCreated(string id)
    {
        using var response = await _client.PostAsJsonAsync("/api/robots", new { id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithExistingId_ReturnsConflictAndPreservesOriginal()
    {
        var api = new RobotApiClient(_client);
        var original = await api.RegisterAsync("R01");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => api.RegisterAsync("R01"));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        var robots = await _client.GetFromJsonAsync<RobotResponse[]>("/api/robots");
        Assert.Equal(original, Assert.Single(robots!));
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        using var response = await _client.GetAsync("/api/robots/UNKNOWN");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithSeveralRobots_ReturnsThemInIdOrder()
    {
        var api = new RobotApiClient(_client);
        await api.RegisterAsync("R02");
        await api.RegisterAsync("R01");

        var robots = await _client.GetFromJsonAsync<RobotResponse[]>("/api/robots");

        Assert.Equal(["R01", "R02"], robots!.Select(robot => robot.Id));
    }

    [Fact]
    public async Task Register_WhenRequestsCompeteForSameId_CreatesExactlyOneRobot()
    {
        var requests = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                using var response = await _client.PostAsJsonAsync("/api/robots", new { id = "R01" });
                return response.StatusCode;
            }));

        var statuses = await Task.WhenAll(requests);

        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.Created));
        Assert.Equal(9, statuses.Count(status => status == HttpStatusCode.Conflict));
        Assert.Single((await _client.GetFromJsonAsync<RobotResponse[]>("/api/robots"))!);
    }

    [Fact]
    public async Task GetById_AfterServerRestart_PreservesRegistration()
    {
        var original = await new RobotApiClient(_client).RegisterAsync("R01");
        _client.Dispose();
        _factory.Dispose();

        using var restartedApi = new TestApiFactory(Path.Combine(_directory, "robots.db"));
        using var restartedClient = restartedApi.CreateClient();
        var recovered = await restartedClient.GetFromJsonAsync<RobotResponse>("/api/robots/R01");

        Assert.Equal(original, recovered);
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

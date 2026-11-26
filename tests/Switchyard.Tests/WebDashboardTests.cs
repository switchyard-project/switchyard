using System.Net;

namespace Switchyard.Tests;

public sealed class WebDashboardTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "switchyard-tests", Guid.NewGuid().ToString("N"));
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public WebDashboardTests()
    {
        _factory = new TestApiFactory(Path.Combine(_directory, "robots.db"));
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData("/", "text/html")]
    [InlineData("/styles.css", "text/css")]
    [InlineData("/dashboard.js", "text/javascript")]
    [InlineData("/robot-api.js", "text/javascript")]
    [InlineData("/assets/switchyard-avatar.svg", "image/svg+xml")]
    public async Task GetPanelResource_ReturnsContentWithExpectedType(string path, string contentType)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData("/appsettings.json")]
    [InlineData("/Program.cs")]
    [InlineData("/api/unknown")]
    public async Task GetNonPublicOrUnknownPath_ReturnsNotFound(string path)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}

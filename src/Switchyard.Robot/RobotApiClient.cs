using System.Net.Http.Json;
using System.Text.Json;
using Switchyard.Contracts;

namespace Switchyard.Robot;

public sealed class RobotApiClient
{
    private readonly HttpClient _httpClient;

    public RobotApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RobotResponse> RegisterAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new RegisterRobotRequest { Id = id };
        using var response = await _httpClient.PostAsJsonAsync("api/robots", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var robot = await response.Content.ReadFromJsonAsync<RobotResponse>(cancellationToken);
        if (robot is null || robot.Id != id || robot.RegisteredAt == default)
        {
            throw new JsonException("La respuesta no contiene el registro esperado.");
        }

        return robot;
    }
}

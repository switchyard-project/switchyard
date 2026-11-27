using System.Net;
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

    public async Task<TransportTaskResponse?> ClaimTaskAsync(
        string robotId, CancellationToken cancellationToken = default)
    {
        var robotPath = $"api/robots/{Uri.EscapeDataString(robotId)}";
        using var response = await _httpClient.PostAsync($"{robotPath}/tasks/claim", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        return await ReadTaskAsync(response, robotId, TransportTaskResponse.Assigned, cancellationToken);
    }

    public async Task<TransportTaskResponse> CompleteTaskAsync(
        string robotId, long taskId, CancellationToken cancellationToken = default)
    {
        var robotPath = $"api/robots/{Uri.EscapeDataString(robotId)}";
        using var response = await _httpClient.PutAsync(
            $"{robotPath}/tasks/{taskId}/completion", null, cancellationToken);
        var task = await ReadTaskAsync(response, robotId, TransportTaskResponse.Completed, cancellationToken);
        if (task.Id != taskId)
        {
            throw new JsonException("La respuesta corresponde a otra tarea.");
        }

        return task;
    }

    private static async Task<TransportTaskResponse> ReadTaskAsync(
        HttpResponseMessage response, string robotId, string status, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        var task = await response.Content.ReadFromJsonAsync<TransportTaskResponse>(cancellationToken);
        if (task is null || task.Id <= 0 || task.RobotId != robotId || task.Status != status
            || string.IsNullOrWhiteSpace(task.Pickup) || string.IsNullOrWhiteSpace(task.Destination))
        {
            throw new JsonException("La respuesta no contiene la tarea esperada.");
        }

        return task;
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Switchyard.Robot;

namespace Switchyard.Tests;

public sealed class RobotApiClientTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData("{\"id\":\"R01\"}")]
    [InlineData("{\"id\":\"OTHER\",\"registeredAt\":\"2026-11-25T12:00:00Z\"}")]
    public async Task RegisterAsync_WithUnexpectedResponse_DoesNotReportSuccess(string json)
    {
        using var httpClient = new HttpClient(new FixedResponseHandler(json))
        {
            BaseAddress = new Uri("http://localhost")
        };
        var api = new RobotApiClient(httpClient);

        await Assert.ThrowsAsync<JsonException>(() => api.RegisterAsync("R01"));
    }

    private sealed class FixedResponseHandler : HttpMessageHandler
    {
        private readonly string _json;

        public FixedResponseHandler(string json)
        {
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
        }
    }
}

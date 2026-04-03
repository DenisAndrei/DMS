using System.Net;
using System.Net.Http.Json;

namespace DeviceManagement.Api.IntegrationTests;

public sealed class RootEndpointTests : IClassFixture<ApiApplicationFactory>
{
    private readonly HttpClient _client;

    public RootEndpointTests(ApiApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ReturnsPhaseOneMetadata()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RootPayload>();

        Assert.NotNull(payload);
        Assert.Equal("Device Management API", payload!.Name);
        Assert.Equal("Phase 1", payload.Phase);
        Assert.Contains("/api/users", payload.Endpoints);
        Assert.Contains("/api/devices", payload.Endpoints);
    }

    private sealed record RootPayload(string Name, string Phase, string[] Endpoints);
}

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
    public async Task GetRoot_ReturnsPhaseThreeMetadata()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RootPayload>();

        Assert.NotNull(payload);
        Assert.Equal("Device Management API", payload!.Name);
        Assert.Equal("Phase 3", payload.Phase);
        Assert.Contains("/api/auth/register", payload.Endpoints);
        Assert.Contains("/api/auth/login", payload.Endpoints);
        Assert.Contains("/api/users", payload.Endpoints);
        Assert.Contains("/api/devices", payload.Endpoints);
        Assert.Contains("/api/devices/{id}/assign", payload.Endpoints);
        Assert.Contains("/api/devices/{id}/unassign", payload.Endpoints);
    }

    private sealed record RootPayload(string Name, string Phase, string[] Endpoints);
}

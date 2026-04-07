using System.Net;

namespace DeviceManagement.Api.IntegrationTests;

public sealed class RootEndpointTests : IClassFixture<ApiApplicationFactory>
{
    private readonly HttpClient _client;

    public RootEndpointTests(ApiApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ReturnsOk()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

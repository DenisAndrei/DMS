using System.Net;
using System.Net.Http.Json;

namespace DeviceManagement.Api.IntegrationTests;

public sealed class DeviceSecurityTests : IClassFixture<ApiApplicationFactory>
{
    private readonly HttpClient _client;

    public DeviceSecurityTests(ApiApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostDevice_WithoutJwtToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/devices",
            new
            {
                name = "Atlas Phone",
                manufacturer = "Darwin",
                type = 0,
                operatingSystem = "Android",
                osVersion = "15",
                processor = "Tensor G5",
                ramAmountGb = 12,
                description = "Managed company handset.",
                location = "Bucharest Office",
                assignedUserId = (int?)null
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

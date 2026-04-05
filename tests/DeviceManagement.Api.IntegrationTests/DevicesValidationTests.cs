using System.Net;
using System.Net.Http.Json;

namespace DeviceManagement.Api.IntegrationTests;

public sealed class DevicesValidationTests : IClassFixture<ApiApplicationFactory>
{
    private readonly HttpClient _client;

    public DevicesValidationTests(ApiApplicationFactory factory)
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
                name = "",
                manufacturer = "",
                type = 0,
                operatingSystem = "",
                osVersion = "",
                processor = "",
                ramAmountGb = 0,
                description = "",
                location = "",
                assignedUserId = (int?)null
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

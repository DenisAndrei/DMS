using System.Net;
using System.Net.Http.Json;

namespace DeviceManagement.Api.IntegrationTests;

public sealed class UsersValidationTests : IClassFixture<ApiApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersValidationTests(ApiApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostUser_WithMissingRequiredValues_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/users",
            new
            {
                name = "",
                role = "",
                location = ""
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

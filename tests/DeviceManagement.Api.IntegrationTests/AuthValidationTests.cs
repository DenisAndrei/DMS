using System.Net;
using System.Net.Http.Json;

namespace DeviceManagement.Api.IntegrationTests;

public sealed class AuthValidationTests : IClassFixture<ApiApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthValidationTests(ApiApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithMissingRequiredFields_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "",
                password = ""
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

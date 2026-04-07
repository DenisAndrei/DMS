using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeviceManagement.Api.Contracts.Requests;
using DeviceManagement.Api.Contracts.Responses;

namespace DeviceManagement.Api.IntegrationTests;

public sealed class AuthenticatedFlowTests : IClassFixture<AuthenticatedApiApplicationFactory>
{
    private static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly AuthenticatedApiApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthenticatedFlowTests(AuthenticatedApiApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ThenGetProtectedDevices_ReturnsOk()
    {
        var auth = await RegisterAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await _client.GetAsync("/api/devices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<DeviceResponse>>(ApiJsonOptions);
        Assert.NotNull(payload);
        Assert.Empty(payload!);
    }

    [Fact]
    public async Task AuthenticatedUser_CanCreateAndAssignDevice()
    {
        var auth = await RegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/devices",
            new CreateDeviceRequest
            {
                Name = "Atlas Phone",
                Manufacturer = "Darwin",
                Type = DeviceManagement.Api.Domain.DeviceType.Phone,
                OperatingSystem = "Android",
                OsVersion = "15",
                Processor = "Tensor G5",
                RamAmountGb = 12,
                Description = "Managed company handset.",
                Location = "Bucharest Office"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdDevice = await createResponse.Content.ReadFromJsonAsync<DeviceResponse>(ApiJsonOptions);
        Assert.NotNull(createdDevice);
        Assert.Null(createdDevice!.AssignedUserId);

        var assignResponse = await _client.PostAsync($"/api/devices/{createdDevice.Id}/assign", null);

        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var assignedDevice = await assignResponse.Content.ReadFromJsonAsync<DeviceResponse>(ApiJsonOptions);
        Assert.NotNull(assignedDevice);
        Assert.Equal(auth.User.UserId, assignedDevice!.AssignedUserId);
        Assert.Equal(auth.User.Name, assignedDevice.AssignedUser?.Name);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        await RegisterAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = "alexandra.ionescu@darwin.local",
                Password = "WrongPass123!"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<AuthResponse> RegisterAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest
            {
                Email = "alexandra.ionescu@darwin.local",
                Password = "Darwin123!"
            });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var payload = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(ApiJsonOptions);
        Assert.NotNull(payload);
        return payload!;
    }
}

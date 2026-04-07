using System.Text.Json;
using System.Text.Json.Serialization;
using DeviceManagement.Api.Infrastructure.Data;
using DeviceManagement.Api.Infrastructure.Repositories;
using DeviceManagement.Api.Middleware;
using DeviceManagement.Api.Options;
using DeviceManagement.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var authOptions = builder.Configuration
    .GetSection(AuthOptions.SectionName)
    .Get<AuthOptions>()
    ?? throw new InvalidOperationException("Authentication configuration is missing.");

builder.Services.AddProblemDetails();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = authOptions.Issuer,
            ValidAudience = authOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(authOptions.GetSigningKeyBytes()),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddHttpClient<IDeviceDescriptionGenerator, OllamaDeviceDescriptionGenerator>(
    (serviceProvider, client) =>
    {
        var aiOptions = serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value;
        client.BaseAddress = new Uri(aiOptions.OllamaBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, aiOptions.RequestTimeoutSeconds));
    });

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    name = "Device Management API",
    phase = "Phase 3",
    endpoints = new[]
    {
        "/api/auth/register",
        "/api/auth/login",
        "/api/users",
        "/api/devices",
        "/api/devices/search?q={query}",
        "/api/devices/generate-description",
        "/api/devices/{id}/assign",
        "/api/devices/{id}/unassign"
    }
}));

app.MapControllers();

app.Run();

public partial class Program;

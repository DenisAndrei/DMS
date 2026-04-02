using DeviceManagement.Api.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

var app = builder.Build();

app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    name = "Device Management API",
    phase = "Phase 1",
    endpoints = new[]
    {
        "/api/users",
        "/api/devices"
    }
}));

app.MapControllers();

app.Run();

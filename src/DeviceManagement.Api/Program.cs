using DeviceManagement.Api.Infrastructure.Data;
using DeviceManagement.Api.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

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

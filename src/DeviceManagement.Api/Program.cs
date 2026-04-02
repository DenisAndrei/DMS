var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

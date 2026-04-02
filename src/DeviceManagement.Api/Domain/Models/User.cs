namespace DeviceManagement.Api.Domain.Models;

public sealed record User(
    int Id,
    string Name,
    string Role,
    string Location,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

namespace DeviceManagement.Api.Contracts.Responses;

public sealed record UserResponse(
    int Id,
    string Name,
    string Role,
    string Location,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

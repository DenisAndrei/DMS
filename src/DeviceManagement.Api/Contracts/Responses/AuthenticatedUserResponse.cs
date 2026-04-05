namespace DeviceManagement.Api.Contracts.Responses;

public sealed record AuthenticatedUserResponse(
    int UserId,
    string Email,
    string Name,
    string Role,
    string Location);

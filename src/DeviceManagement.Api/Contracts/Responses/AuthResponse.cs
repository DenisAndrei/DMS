namespace DeviceManagement.Api.Contracts.Responses;

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    AuthenticatedUserResponse User);

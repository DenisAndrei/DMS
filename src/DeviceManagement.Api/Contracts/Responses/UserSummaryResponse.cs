namespace DeviceManagement.Api.Contracts.Responses;

public sealed record UserSummaryResponse(
    int Id,
    string Name,
    string Role,
    string Location);

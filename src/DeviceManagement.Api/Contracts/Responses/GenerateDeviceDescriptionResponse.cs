namespace DeviceManagement.Api.Contracts.Responses;

public sealed record GenerateDeviceDescriptionResponse(
    string Description,
    string Provider,
    string Model,
    bool UsedFallback);

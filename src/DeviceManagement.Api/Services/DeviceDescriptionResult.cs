namespace DeviceManagement.Api.Services;

public sealed record DeviceDescriptionResult(
    string Description,
    string Provider,
    string Model,
    bool UsedFallback);

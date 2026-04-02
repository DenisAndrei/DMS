using DeviceManagement.Api.Domain;

namespace DeviceManagement.Api.Contracts.Responses;

public sealed record DeviceResponse(
    int Id,
    string Name,
    string Manufacturer,
    DeviceType Type,
    string OperatingSystem,
    string OsVersion,
    string Processor,
    int RamAmountGb,
    string Description,
    string Location,
    int? AssignedUserId,
    UserSummaryResponse? AssignedUser,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

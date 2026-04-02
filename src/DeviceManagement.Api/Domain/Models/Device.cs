namespace DeviceManagement.Api.Domain.Models;

public sealed record Device(
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
    User? AssignedUser,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

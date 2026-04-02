namespace DeviceManagement.Api.Domain.Models;

public sealed record DeviceUpsertModel(
    string Name,
    string Manufacturer,
    DeviceType Type,
    string OperatingSystem,
    string OsVersion,
    string Processor,
    int RamAmountGb,
    string Description,
    string Location,
    int? AssignedUserId);

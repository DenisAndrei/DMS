namespace DeviceManagement.Api.Domain.Models;

public sealed record DeviceDescriptionInput(
    string Name,
    string Manufacturer,
    DeviceType Type,
    string OperatingSystem,
    string OsVersion,
    string Processor,
    int RamAmountGb);

using System.ComponentModel.DataAnnotations;
using DeviceManagement.Api.Domain;

namespace DeviceManagement.Api.Contracts.Requests;

public sealed class CreateDeviceRequest
{
    [Required]
    [StringLength(120)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Manufacturer { get; init; } = string.Empty;

    [Required]
    [EnumDataType(typeof(DeviceType))]
    public DeviceType Type { get; init; }

    [Required]
    [StringLength(120)]
    public string OperatingSystem { get; init; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string OsVersion { get; init; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Processor { get; init; } = string.Empty;

    [Range(1, 1024)]
    public int RamAmountGb { get; init; }

    [Required]
    [StringLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Location { get; init; } = string.Empty;

    public int? AssignedUserId { get; init; }
}

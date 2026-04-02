using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Api.Contracts.Requests;

public sealed class CreateUserRequest
{
    [Required]
    [StringLength(120)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Role { get; init; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Location { get; init; } = string.Empty;
}

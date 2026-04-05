namespace DeviceManagement.Api.Domain.Models;

public sealed record AuthAccount(
    int AccountId,
    int UserId,
    string Email,
    string PasswordHash,
    string PasswordSalt,
    string Name,
    string Role,
    string Location);

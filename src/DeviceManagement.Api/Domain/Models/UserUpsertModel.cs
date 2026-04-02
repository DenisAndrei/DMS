namespace DeviceManagement.Api.Domain.Models;

public sealed record UserUpsertModel(
    string Name,
    string Role,
    string Location);

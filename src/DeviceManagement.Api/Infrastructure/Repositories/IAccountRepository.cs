using DeviceManagement.Api.Domain.Models;

namespace DeviceManagement.Api.Infrastructure.Repositories;

public interface IAccountRepository
{
    Task<AuthAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<AuthAccount> CreateAsync(
        string email,
        string passwordHash,
        string passwordSalt,
        UserUpsertModel userProfile,
        CancellationToken cancellationToken);
}

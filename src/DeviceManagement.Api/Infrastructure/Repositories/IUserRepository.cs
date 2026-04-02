using DeviceManagement.Api.Domain.Models;

namespace DeviceManagement.Api.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(UserUpsertModel user, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, UserUpsertModel user, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<bool> HasAssignedDevicesAsync(int id, CancellationToken cancellationToken);
}

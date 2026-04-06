using DeviceManagement.Api.Domain.Models;

namespace DeviceManagement.Api.Infrastructure.Repositories;

public interface IDeviceRepository
{
    Task<IReadOnlyCollection<Device>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Device>> SearchAsync(string query, CancellationToken cancellationToken);

    Task<Device?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<int> CreateAsync(DeviceUpsertModel device, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(int id, DeviceUpsertModel device, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    Task<bool> AssignAsync(int id, int userId, CancellationToken cancellationToken);

    Task<bool> UnassignAsync(int id, int userId, CancellationToken cancellationToken);

    Task<bool> ExistsDuplicateAsync(
        string name,
        string manufacturer,
        string type,
        string operatingSystem,
        string osVersion,
        int? excludeId,
        CancellationToken cancellationToken);
}

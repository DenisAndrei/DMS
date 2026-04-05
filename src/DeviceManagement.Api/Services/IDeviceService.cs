using DeviceManagement.Api.Contracts.Requests;
using DeviceManagement.Api.Contracts.Responses;

namespace DeviceManagement.Api.Services;

public interface IDeviceService
{
    Task<IReadOnlyCollection<DeviceResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<DeviceResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken);

    Task<DeviceResponse> UpdateAsync(int id, UpdateDeviceRequest request, CancellationToken cancellationToken);

    Task<DeviceResponse> AssignToUserAsync(int id, int userId, CancellationToken cancellationToken);

    Task<DeviceResponse> UnassignFromUserAsync(int id, int userId, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

using DeviceManagement.Api.Contracts.Requests;
using DeviceManagement.Api.Contracts.Responses;

namespace DeviceManagement.Api.Services;

public interface IUserService
{
    Task<IReadOnlyCollection<UserResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

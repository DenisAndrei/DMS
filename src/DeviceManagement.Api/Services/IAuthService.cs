using DeviceManagement.Api.Contracts.Requests;
using DeviceManagement.Api.Contracts.Responses;

namespace DeviceManagement.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

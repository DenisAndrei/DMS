using DeviceManagement.Api.Domain.Models;

namespace DeviceManagement.Api.Services;

public interface IDeviceDescriptionGenerator
{
    Task<DeviceDescriptionResult> GenerateAsync(
        DeviceDescriptionInput input,
        CancellationToken cancellationToken);
}

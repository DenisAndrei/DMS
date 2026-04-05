using DeviceManagement.Api.Contracts.Requests;
using DeviceManagement.Api.Contracts.Responses;
using DeviceManagement.Api.Domain;
using DeviceManagement.Api.Domain.Exceptions;
using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Infrastructure.Repositories;

namespace DeviceManagement.Api.Services;

public sealed class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUserRepository _userRepository;

    public DeviceService(IDeviceRepository deviceRepository, IUserRepository userRepository)
    {
        _deviceRepository = deviceRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<DeviceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var devices = await _deviceRepository.GetAllAsync(cancellationToken);
        return devices.Select(MapDeviceResponse).ToArray();
    }

    public async Task<DeviceResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(id, cancellationToken);
        if (device is null)
        {
            throw new EntityNotFoundException("Device", id);
        }

        return MapDeviceResponse(device);
    }

    public async Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken)
    {
        var deviceUpsertModel = await BuildUpsertModelAsync(
            request.Name,
            request.Manufacturer,
            request.Type,
            request.OperatingSystem,
            request.OsVersion,
            request.Processor,
            request.RamAmountGb,
            request.Description,
            request.Location,
            request.AssignedUserId,
            null,
            cancellationToken);

        var createdDeviceId = await _deviceRepository.CreateAsync(deviceUpsertModel, cancellationToken);
        var createdDevice = await _deviceRepository.GetByIdAsync(createdDeviceId, cancellationToken);

        if (createdDevice is null)
        {
            throw new EntityNotFoundException("Device", createdDeviceId);
        }

        return MapDeviceResponse(createdDevice);
    }

    public async Task<DeviceResponse> UpdateAsync(int id, UpdateDeviceRequest request, CancellationToken cancellationToken)
    {
        var existingDevice = await _deviceRepository.GetByIdAsync(id, cancellationToken);
        if (existingDevice is null)
        {
            throw new EntityNotFoundException("Device", id);
        }

        var deviceUpsertModel = await BuildUpsertModelAsync(
            request.Name,
            request.Manufacturer,
            request.Type,
            request.OperatingSystem,
            request.OsVersion,
            request.Processor,
            request.RamAmountGb,
            request.Description,
            request.Location,
            existingDevice.AssignedUserId,
            id,
            cancellationToken);

        await _deviceRepository.UpdateAsync(id, deviceUpsertModel, cancellationToken);

        var updatedDevice = await _deviceRepository.GetByIdAsync(id, cancellationToken);
        if (updatedDevice is null)
        {
            throw new EntityNotFoundException("Device", id);
        }

        return MapDeviceResponse(updatedDevice);
    }

    public async Task<DeviceResponse> AssignToUserAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(id, cancellationToken);
        if (device is null)
        {
            throw new EntityNotFoundException("Device", id);
        }

        if (device.AssignedUserId == userId)
        {
            return MapDeviceResponse(device);
        }

        if (device.AssignedUserId.HasValue)
        {
            throw new ConflictException("The device is already assigned to another user.");
        }

        var assigned = await _deviceRepository.AssignAsync(id, userId, cancellationToken);
        if (!assigned)
        {
            throw new ConflictException("The device could not be assigned.");
        }

        var updatedDevice = await _deviceRepository.GetByIdAsync(id, cancellationToken);
        if (updatedDevice is null)
        {
            throw new EntityNotFoundException("Device", id);
        }

        return MapDeviceResponse(updatedDevice);
    }

    public async Task<DeviceResponse> UnassignFromUserAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(id, cancellationToken);
        if (device is null)
        {
            throw new EntityNotFoundException("Device", id);
        }

        if (!device.AssignedUserId.HasValue)
        {
            throw new ConflictException("The device is not assigned to any user.");
        }

        if (device.AssignedUserId.Value != userId)
        {
            throw new ForbiddenException("You can only unassign a device assigned to your account.");
        }

        var unassigned = await _deviceRepository.UnassignAsync(id, userId, cancellationToken);
        if (!unassigned)
        {
            throw new ConflictException("The device could not be unassigned.");
        }

        var updatedDevice = await _deviceRepository.GetByIdAsync(id, cancellationToken);
        if (updatedDevice is null)
        {
            throw new EntityNotFoundException("Device", id);
        }

        return MapDeviceResponse(updatedDevice);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _deviceRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new EntityNotFoundException("Device", id);
        }
    }

    private async Task<DeviceUpsertModel> BuildUpsertModelAsync(
        string? name,
        string? manufacturer,
        DeviceType type,
        string? operatingSystem,
        string? osVersion,
        string? processor,
        int ramAmountGb,
        string? description,
        string? location,
        int? assignedUserId,
        int? excludedDeviceId,
        CancellationToken cancellationToken)
    {
        ValidateDeviceType(type);

        var normalizedName = NormalizeRequiredText(name, nameof(name));
        var normalizedManufacturer = NormalizeRequiredText(manufacturer, nameof(manufacturer));
        var normalizedOperatingSystem = NormalizeRequiredText(operatingSystem, nameof(operatingSystem));
        var normalizedOsVersion = NormalizeRequiredText(osVersion, nameof(osVersion));
        var normalizedProcessor = NormalizeRequiredText(processor, nameof(processor));
        var normalizedDescription = NormalizeRequiredText(description, nameof(description));
        var normalizedLocation = NormalizeRequiredText(location, nameof(location));

        if (ramAmountGb <= 0)
        {
            throw new RequestValidationException(nameof(ramAmountGb), "RAM amount must be greater than 0.");
        }

        if (assignedUserId.HasValue &&
            !await _userRepository.ExistsAsync(assignedUserId.Value, cancellationToken))
        {
            throw new RequestValidationException(nameof(assignedUserId), "Assigned user does not exist.");
        }

        var hasDuplicate = await _deviceRepository.ExistsDuplicateAsync(
            normalizedName,
            normalizedManufacturer,
            type.ToDatabaseValue(),
            normalizedOperatingSystem,
            normalizedOsVersion,
            excludedDeviceId,
            cancellationToken);

        if (hasDuplicate)
        {
            throw new ConflictException(
                "A device with the same name, manufacturer, type, operating system, and OS version already exists.");
        }

        return new DeviceUpsertModel(
            normalizedName,
            normalizedManufacturer,
            type,
            normalizedOperatingSystem,
            normalizedOsVersion,
            normalizedProcessor,
            ramAmountGb,
            normalizedDescription,
            normalizedLocation,
            assignedUserId);
    }

    private static void ValidateDeviceType(DeviceType deviceType)
    {
        if (!Enum.IsDefined(deviceType))
        {
            throw new RequestValidationException(nameof(DeviceType), "Type must be phone or tablet.");
        }
    }

    private static string NormalizeRequiredText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RequestValidationException(fieldName, $"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static DeviceResponse MapDeviceResponse(Device device) =>
        new(
            device.Id,
            device.Name,
            device.Manufacturer,
            device.Type,
            device.OperatingSystem,
            device.OsVersion,
            device.Processor,
            device.RamAmountGb,
            device.Description,
            device.Location,
            device.AssignedUserId,
            device.AssignedUser is null
                ? null
                : new UserSummaryResponse(
                    device.AssignedUser.Id,
                    device.AssignedUser.Name,
                    device.AssignedUser.Role,
                    device.AssignedUser.Location),
            device.CreatedAtUtc,
            device.UpdatedAtUtc);
}

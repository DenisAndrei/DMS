using DeviceManagement.Api.Contracts.Requests;
using DeviceManagement.Api.Domain;
using DeviceManagement.Api.Domain.Exceptions;
using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Infrastructure.Repositories;
using DeviceManagement.Api.Services;

namespace DeviceManagement.Api.UnitTests;

public sealed class DeviceServiceTests
{
    [Fact]
    public async Task UpdateAsync_PreservesTheExistingAssignment()
    {
        var deviceRepository = new FakeDeviceRepository(BuildDevice(5, assignedUserId: 7));
        var userRepository = new FakeUserRepository();
        userRepository.AddExistingUser(7);
        var service = new DeviceService(deviceRepository, userRepository);

        var response = await service.UpdateAsync(
            5,
            new UpdateDeviceRequest
            {
                Name = "Atlas Phone",
                Manufacturer = "Darwin",
                Type = DeviceType.Phone,
                OperatingSystem = "Android",
                OsVersion = "15.1",
                Processor = "Tensor G5",
                RamAmountGb = 12,
                Description = "Updated device details.",
                Location = "Cluj Office"
            },
            CancellationToken.None);

        Assert.NotNull(deviceRepository.LastUpdatedModel);
        Assert.Equal(7, deviceRepository.LastUpdatedModel!.AssignedUserId);
        Assert.Equal(7, response.AssignedUserId);
        Assert.Equal("Cluj Office", response.Location);
    }

    [Fact]
    public async Task UnassignFromUserAsync_WhenDeviceBelongsToAnotherUser_ThrowsForbidden()
    {
        var service = new DeviceService(
            new FakeDeviceRepository(BuildDevice(3, assignedUserId: 99)),
            new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.UnassignFromUserAsync(3, 7, CancellationToken.None));
    }

    private static Device BuildDevice(int id, int? assignedUserId = null)
    {
        var assignedUser = assignedUserId.HasValue
            ? new User(
                assignedUserId.Value,
                "Assigned User",
                "Employee",
                "Remote",
                DateTime.UtcNow,
                DateTime.UtcNow)
            : null;

        return new Device(
            id,
            "Atlas Phone",
            "Darwin",
            DeviceType.Phone,
            "Android",
            "15",
            "Tensor G5",
            12,
            "Managed company handset.",
            "Bucharest Office",
            assignedUserId,
            assignedUser,
            DateTime.UtcNow,
            DateTime.UtcNow);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly HashSet<int> _existingUserIds = new();

        public void AddExistingUser(int id) => _existingUserIds.Add(id);

        public Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<User>>(Array.Empty<User>());

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(null);

        public Task<int> CreateAsync(UserUpsertModel user, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<bool> UpdateAsync(int id, UserUpsertModel user, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_existingUserIds.Contains(id));

        public Task<bool> HasAssignedDevicesAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }

    private sealed class FakeDeviceRepository : IDeviceRepository
    {
        private readonly Dictionary<int, Device> _devices = new();

        public FakeDeviceRepository(Device device)
        {
            _devices[device.Id] = device;
        }

        public DeviceUpsertModel? LastUpdatedModel { get; private set; }

        public Task<IReadOnlyCollection<Device>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Device>>(_devices.Values.ToArray());

        public Task<IReadOnlyCollection<Device>> SearchAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Device>>(_devices.Values.ToArray());

        public Task<Device?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_devices.GetValueOrDefault(id));

        public Task<int> CreateAsync(DeviceUpsertModel device, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<bool> UpdateAsync(int id, DeviceUpsertModel device, CancellationToken cancellationToken)
        {
            LastUpdatedModel = device;

            if (!_devices.TryGetValue(id, out var existing))
            {
                return Task.FromResult(false);
            }

            _devices[id] = existing with
            {
                Name = device.Name,
                Manufacturer = device.Manufacturer,
                Type = device.Type,
                OperatingSystem = device.OperatingSystem,
                OsVersion = device.OsVersion,
                Processor = device.Processor,
                RamAmountGb = device.RamAmountGb,
                Description = device.Description,
                Location = device.Location,
                AssignedUserId = device.AssignedUserId,
                UpdatedAtUtc = DateTime.UtcNow
            };

            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> AssignAsync(int id, int userId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> UnassignAsync(int id, int userId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> ExistsDuplicateAsync(
            string name,
            string manufacturer,
            string type,
            string operatingSystem,
            string osVersion,
            int? excludeId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}

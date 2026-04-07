using DeviceManagement.Api.Domain;
using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Infrastructure.Repositories;
using DeviceManagement.Api.Services;

namespace DeviceManagement.Api.IntegrationTests;

internal sealed class CompactTestStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<int, User> _users = new();
    private readonly Dictionary<int, AuthAccount> _accounts = new();
    private readonly Dictionary<int, StoredDevice> _devices = new();
    private int _nextUserId = 1;
    private int _nextAccountId = 1;
    private int _nextDeviceId = 1;

    public void Reset()
    {
        lock (_syncRoot)
        {
            _users.Clear();
            _accounts.Clear();
            _devices.Clear();
            _nextUserId = 1;
            _nextAccountId = 1;
            _nextDeviceId = 1;
        }
    }

    public AuthAccount? GetAccountByEmail(string email)
    {
        lock (_syncRoot)
        {
            return _accounts.Values.FirstOrDefault(
                account => string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase));
        }
    }

    public AuthAccount CreateAccount(
        string email,
        string passwordHash,
        string passwordSalt,
        UserUpsertModel profile)
    {
        lock (_syncRoot)
        {
            var now = DateTime.UtcNow;
            var user = new User(_nextUserId++, profile.Name, profile.Role, profile.Location, now, now);
            _users[user.Id] = user;

            var account = new AuthAccount(
                _nextAccountId++,
                user.Id,
                email,
                passwordHash,
                passwordSalt,
                user.Name,
                user.Role,
                user.Location);

            _accounts[account.AccountId] = account;
            return account;
        }
    }

    public bool UserExists(int id)
    {
        lock (_syncRoot)
        {
            return _users.ContainsKey(id);
        }
    }

    public IReadOnlyCollection<User> GetAllUsers()
    {
        lock (_syncRoot)
        {
            return _users.Values.ToArray();
        }
    }

    public User? GetUserById(int id)
    {
        lock (_syncRoot)
        {
            return _users.GetValueOrDefault(id);
        }
    }

    public IReadOnlyCollection<Device> GetAllDevices()
    {
        lock (_syncRoot)
        {
            return _devices.Values.Select(MapDevice).ToArray();
        }
    }

    public IReadOnlyCollection<Device> SearchDevices(string query) => GetAllDevices();

    public Device? GetDeviceById(int id)
    {
        lock (_syncRoot)
        {
            return _devices.TryGetValue(id, out var device) ? MapDevice(device) : null;
        }
    }

    public int CreateDevice(DeviceUpsertModel model)
    {
        lock (_syncRoot)
        {
            var now = DateTime.UtcNow;
            var device = new StoredDevice(
                _nextDeviceId++,
                model.Name,
                model.Manufacturer,
                model.Type,
                model.OperatingSystem,
                model.OsVersion,
                model.Processor,
                model.RamAmountGb,
                model.Description,
                model.Location,
                model.AssignedUserId,
                now,
                now);

            _devices[device.Id] = device;
            return device.Id;
        }
    }

    public bool UpdateDevice(int id, DeviceUpsertModel model)
    {
        lock (_syncRoot)
        {
            if (!_devices.TryGetValue(id, out var existing))
            {
                return false;
            }

            _devices[id] = existing with
            {
                Name = model.Name,
                Manufacturer = model.Manufacturer,
                Type = model.Type,
                OperatingSystem = model.OperatingSystem,
                OsVersion = model.OsVersion,
                Processor = model.Processor,
                RamAmountGb = model.RamAmountGb,
                Description = model.Description,
                Location = model.Location,
                AssignedUserId = model.AssignedUserId,
                UpdatedAtUtc = DateTime.UtcNow
            };

            return true;
        }
    }

    public bool DeleteDevice(int id)
    {
        lock (_syncRoot)
        {
            return _devices.Remove(id);
        }
    }

    public bool AssignDevice(int id, int userId)
    {
        lock (_syncRoot)
        {
            if (!_devices.TryGetValue(id, out var existing) || existing.AssignedUserId.HasValue)
            {
                return false;
            }

            _devices[id] = existing with
            {
                AssignedUserId = userId,
                UpdatedAtUtc = DateTime.UtcNow
            };

            return true;
        }
    }

    public bool UnassignDevice(int id, int userId)
    {
        lock (_syncRoot)
        {
            if (!_devices.TryGetValue(id, out var existing) || existing.AssignedUserId != userId)
            {
                return false;
            }

            _devices[id] = existing with
            {
                AssignedUserId = null,
                UpdatedAtUtc = DateTime.UtcNow
            };

            return true;
        }
    }

    public bool ExistsDuplicate(
        string name,
        string manufacturer,
        string type,
        string operatingSystem,
        string osVersion,
        int? excludeId)
    {
        lock (_syncRoot)
        {
            return _devices.Values.Any(device =>
                device.Id != excludeId
                && string.Equals(device.Name, name, StringComparison.Ordinal)
                && string.Equals(device.Manufacturer, manufacturer, StringComparison.Ordinal)
                && string.Equals(device.Type.ToDatabaseValue(), type, StringComparison.Ordinal)
                && string.Equals(device.OperatingSystem, operatingSystem, StringComparison.Ordinal)
                && string.Equals(device.OsVersion, osVersion, StringComparison.Ordinal));
        }
    }

    private Device MapDevice(StoredDevice device)
    {
        User? assignedUser = null;
        if (device.AssignedUserId.HasValue && _users.TryGetValue(device.AssignedUserId.Value, out var user))
        {
            assignedUser = user;
        }

        return new Device(
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
            assignedUser,
            device.CreatedAtUtc,
            device.UpdatedAtUtc);
    }

    private sealed record StoredDevice(
        int Id,
        string Name,
        string Manufacturer,
        Domain.DeviceType Type,
        string OperatingSystem,
        string OsVersion,
        string Processor,
        int RamAmountGb,
        string Description,
        string Location,
        int? AssignedUserId,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);
}

internal sealed class CompactAccountRepository : IAccountRepository
{
    private readonly CompactTestStore _store;

    public CompactAccountRepository(CompactTestStore store)
    {
        _store = store;
    }

    public Task<AuthAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_store.GetAccountByEmail(email));

    public Task<AuthAccount> CreateAsync(
        string email,
        string passwordHash,
        string passwordSalt,
        UserUpsertModel userProfile,
        CancellationToken cancellationToken) =>
        Task.FromResult(_store.CreateAccount(email, passwordHash, passwordSalt, userProfile));
}

internal sealed class CompactUserRepository : IUserRepository
{
    private readonly CompactTestStore _store;

    public CompactUserRepository(CompactTestStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_store.GetAllUsers());

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_store.GetUserById(id));

    public Task<int> CreateAsync(UserUpsertModel user, CancellationToken cancellationToken) =>
        Task.FromResult(0);

    public Task<bool> UpdateAsync(int id, UserUpsertModel user, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_store.UserExists(id));

    public Task<bool> HasAssignedDevicesAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}

internal sealed class CompactDeviceRepository : IDeviceRepository
{
    private readonly CompactTestStore _store;

    public CompactDeviceRepository(CompactTestStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyCollection<Device>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_store.GetAllDevices());

    public Task<IReadOnlyCollection<Device>> SearchAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult(_store.SearchDevices(query));

    public Task<Device?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_store.GetDeviceById(id));

    public Task<int> CreateAsync(DeviceUpsertModel device, CancellationToken cancellationToken) =>
        Task.FromResult(_store.CreateDevice(device));

    public Task<bool> UpdateAsync(int id, DeviceUpsertModel device, CancellationToken cancellationToken) =>
        Task.FromResult(_store.UpdateDevice(id, device));

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_store.DeleteDevice(id));

    public Task<bool> AssignAsync(int id, int userId, CancellationToken cancellationToken) =>
        Task.FromResult(_store.AssignDevice(id, userId));

    public Task<bool> UnassignAsync(int id, int userId, CancellationToken cancellationToken) =>
        Task.FromResult(_store.UnassignDevice(id, userId));

    public Task<bool> ExistsDuplicateAsync(
        string name,
        string manufacturer,
        string type,
        string operatingSystem,
        string osVersion,
        int? excludeId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_store.ExistsDuplicate(name, manufacturer, type, operatingSystem, osVersion, excludeId));
}

internal sealed class CompactDescriptionGenerator : IDeviceDescriptionGenerator
{
    public Task<DeviceDescriptionResult> GenerateAsync(
        DeviceDescriptionInput input,
        CancellationToken cancellationToken) =>
        Task.FromResult(new DeviceDescriptionResult("Stub description.", "Test Stub", "stub", true));
}

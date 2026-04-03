using DeviceManagement.Api.Contracts.Requests;
using DeviceManagement.Api.Contracts.Responses;
using DeviceManagement.Api.Domain.Exceptions;
using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Infrastructure.Repositories;

namespace DeviceManagement.Api.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<UserResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapUserResponse).ToArray();
    }

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User", id);
        }

        return MapUserResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var userUpsertModel = BuildUpsertModel(request.Name, request.Role, request.Location);
        var createdUserId = await _userRepository.CreateAsync(userUpsertModel, cancellationToken);
        var createdUser = await _userRepository.GetByIdAsync(createdUserId, cancellationToken);

        if (createdUser is null)
        {
            throw new EntityNotFoundException("User", createdUserId);
        }

        return MapUserResponse(createdUser);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (existingUser is null)
        {
            throw new EntityNotFoundException("User", id);
        }

        var userUpsertModel = BuildUpsertModel(request.Name, request.Role, request.Location);
        await _userRepository.UpdateAsync(id, userUpsertModel, cancellationToken);

        var updatedUser = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (updatedUser is null)
        {
            throw new EntityNotFoundException("User", id);
        }

        return MapUserResponse(updatedUser);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (existingUser is null)
        {
            throw new EntityNotFoundException("User", id);
        }

        var hasAssignedDevices = await _userRepository.HasAssignedDevicesAsync(id, cancellationToken);
        if (hasAssignedDevices)
        {
            throw new ConflictException("The user cannot be deleted while devices are still assigned to them.");
        }

        await _userRepository.DeleteAsync(id, cancellationToken);
    }

    private static UserUpsertModel BuildUpsertModel(string? name, string? role, string? location) =>
        new(
            NormalizeRequiredText(name, nameof(name)),
            NormalizeRequiredText(role, nameof(role)),
            NormalizeRequiredText(location, nameof(location)));

    private static string NormalizeRequiredText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RequestValidationException(fieldName, $"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static UserResponse MapUserResponse(User user) =>
        new(
            user.Id,
            user.Name,
            user.Role,
            user.Location,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
}

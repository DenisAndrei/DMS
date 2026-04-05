using System.Security.Cryptography;
using DeviceManagement.Api.Domain.Exceptions;

namespace DeviceManagement.Api.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int IterationCount = 100_000;

    public (string PasswordHash, string PasswordSalt) HashPassword(string password)
    {
        ValidatePassword(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IterationCount,
            HashAlgorithmName.SHA256,
            HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool VerifyPassword(string password, string passwordHash, string passwordSalt)
    {
        ValidatePassword(password);

        var storedHash = Convert.FromBase64String(passwordHash);
        var storedSalt = Convert.FromBase64String(passwordSalt);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            storedSalt,
            IterationCount,
            HashAlgorithmName.SHA256,
            storedHash.Length);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new RequestValidationException(nameof(password), "Password is required.");
        }
    }
}

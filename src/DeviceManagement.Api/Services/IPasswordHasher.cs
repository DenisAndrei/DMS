namespace DeviceManagement.Api.Services;

public interface IPasswordHasher
{
    (string PasswordHash, string PasswordSalt) HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash, string passwordSalt);
}

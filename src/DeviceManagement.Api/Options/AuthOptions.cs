using System.Text;

namespace DeviceManagement.Api.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Authentication";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int TokenLifetimeMinutes { get; init; } = 480;

    public byte[] GetSigningKeyBytes()
    {
        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException("Authentication signing key is missing.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(SigningKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("Authentication signing key must be at least 32 bytes.");
        }

        return keyBytes;
    }
}

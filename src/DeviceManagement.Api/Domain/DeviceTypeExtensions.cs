namespace DeviceManagement.Api.Domain;

public static class DeviceTypeExtensions
{
    public static string ToDatabaseValue(this DeviceType deviceType) =>
        deviceType switch
        {
            DeviceType.Phone => "phone",
            DeviceType.Tablet => "tablet",
            _ => throw new ArgumentOutOfRangeException(nameof(deviceType), deviceType, "Unsupported device type.")
        };

    public static DeviceType FromDatabaseValue(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "phone" => DeviceType.Phone,
            "tablet" => DeviceType.Tablet,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported device type value.")
        };
}

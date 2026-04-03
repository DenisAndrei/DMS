using DeviceManagement.Api.Domain;
using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace DeviceManagement.Api.Infrastructure.Repositories;

public sealed class DeviceRepository : IDeviceRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DeviceRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IReadOnlyCollection<Device>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                d.Id,
                d.Name,
                d.Manufacturer,
                d.Type,
                d.OperatingSystem,
                d.OsVersion,
                d.Processor,
                d.RamAmountGb,
                d.Description,
                d.Location,
                d.AssignedUserId,
                d.CreatedAtUtc,
                d.UpdatedAtUtc,
                u.Id AS AssignedUserEntityId,
                u.Name AS AssignedUserName,
                u.Role AS AssignedUserRole,
                u.Location AS AssignedUserLocation,
                u.CreatedAtUtc AS AssignedUserCreatedAtUtc,
                u.UpdatedAtUtc AS AssignedUserUpdatedAtUtc
            FROM dbo.Devices AS d
            LEFT JOIN dbo.Users AS u
                ON u.Id = d.AssignedUserId
            ORDER BY d.Name, d.Manufacturer;
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var devices = new List<Device>();
        while (await reader.ReadAsync(cancellationToken))
        {
            devices.Add(MapDevice(reader));
        }

        return devices;
    }

    public async Task<Device?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                d.Id,
                d.Name,
                d.Manufacturer,
                d.Type,
                d.OperatingSystem,
                d.OsVersion,
                d.Processor,
                d.RamAmountGb,
                d.Description,
                d.Location,
                d.AssignedUserId,
                d.CreatedAtUtc,
                d.UpdatedAtUtc,
                u.Id AS AssignedUserEntityId,
                u.Name AS AssignedUserName,
                u.Role AS AssignedUserRole,
                u.Location AS AssignedUserLocation,
                u.CreatedAtUtc AS AssignedUserCreatedAtUtc,
                u.UpdatedAtUtc AS AssignedUserUpdatedAtUtc
            FROM dbo.Devices AS d
            LEFT JOIN dbo.Users AS u
                ON u.Id = d.AssignedUserId
            WHERE d.Id = @Id;
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapDevice(reader);
    }

    public async Task<int> CreateAsync(DeviceUpsertModel device, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Devices
            (
                Name,
                Manufacturer,
                Type,
                OperatingSystem,
                OsVersion,
                Processor,
                RamAmountGb,
                Description,
                Location,
                AssignedUserId
            )
            VALUES (@Name, @Manufacturer, @Type, @OperatingSystem, @OsVersion, @Processor, @RamAmountGb, @Description, @Location, @AssignedUserId);

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        AddUpsertParameters(command, device);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(int id, DeviceUpsertModel device, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Devices
            SET
                Name = @Name,
                Manufacturer = @Manufacturer,
                Type = @Type,
                OperatingSystem = @OperatingSystem,
                OsVersion = @OsVersion,
                Processor = @Processor,
                RamAmountGb = @RamAmountGb,
                Description = @Description,
                Location = @Location,
                AssignedUserId = @AssignedUserId,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE Id = @Id;
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        AddUpsertParameters(command, device);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.Devices WHERE Id = @Id;";

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<bool> ExistsDuplicateAsync(
        string name,
        string manufacturer,
        string type,
        string operatingSystem,
        string osVersion,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var sql = """
            SELECT COUNT(1)
            FROM dbo.Devices
            WHERE Name = @Name
              AND Manufacturer = @Manufacturer
              AND Type = @Type
              AND OperatingSystem = @OperatingSystem
              AND OsVersion = @OsVersion
            """;

        sql += excludeId.HasValue
            ? Environment.NewLine + "  AND Id <> @ExcludeId;"
            : ";";

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 120).Value = name;
        command.Parameters.Add("@Manufacturer", System.Data.SqlDbType.NVarChar, 120).Value = manufacturer;
        command.Parameters.Add("@Type", System.Data.SqlDbType.NVarChar, 20).Value = type;
        command.Parameters.Add("@OperatingSystem", System.Data.SqlDbType.NVarChar, 120).Value = operatingSystem;
        command.Parameters.Add("@OsVersion", System.Data.SqlDbType.NVarChar, 50).Value = osVersion;

        if (excludeId.HasValue)
        {
            command.Parameters.Add("@ExcludeId", System.Data.SqlDbType.Int).Value = excludeId.Value;
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static void AddUpsertParameters(SqlCommand command, DeviceUpsertModel device)
    {
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 120).Value = device.Name;
        command.Parameters.Add("@Manufacturer", System.Data.SqlDbType.NVarChar, 120).Value = device.Manufacturer;
        command.Parameters.Add("@Type", System.Data.SqlDbType.NVarChar, 20).Value = device.Type.ToDatabaseValue();
        command.Parameters.Add("@OperatingSystem", System.Data.SqlDbType.NVarChar, 120).Value = device.OperatingSystem;
        command.Parameters.Add("@OsVersion", System.Data.SqlDbType.NVarChar, 50).Value = device.OsVersion;
        command.Parameters.Add("@Processor", System.Data.SqlDbType.NVarChar, 120).Value = device.Processor;
        command.Parameters.Add("@RamAmountGb", System.Data.SqlDbType.Int).Value = device.RamAmountGb;
        command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, 1000).Value = device.Description;
        command.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 120).Value = device.Location;
        command.Parameters.Add("@AssignedUserId", System.Data.SqlDbType.Int).Value = device.AssignedUserId ?? (object)DBNull.Value;
    }

    private static Device MapDevice(SqlDataReader reader)
    {
        User? assignedUser = null;
        var assignedUserId = reader.GetNullableInt32("AssignedUserEntityId");

        if (assignedUserId.HasValue)
        {
            assignedUser = new User(
                assignedUserId.Value,
                reader.GetRequiredString("AssignedUserName"),
                reader.GetRequiredString("AssignedUserRole"),
                reader.GetRequiredString("AssignedUserLocation"),
                reader.GetRequiredDateTime("AssignedUserCreatedAtUtc"),
                reader.GetRequiredDateTime("AssignedUserUpdatedAtUtc"));
        }

        return new Device(
            reader.GetRequiredInt32("Id"),
            reader.GetRequiredString("Name"),
            reader.GetRequiredString("Manufacturer"),
            DeviceTypeExtensions.FromDatabaseValue(reader.GetRequiredString("Type")),
            reader.GetRequiredString("OperatingSystem"),
            reader.GetRequiredString("OsVersion"),
            reader.GetRequiredString("Processor"),
            reader.GetRequiredInt32("RamAmountGb"),
            reader.GetRequiredString("Description"),
            reader.GetRequiredString("Location"),
            reader.GetNullableInt32("AssignedUserId"),
            assignedUser,
            reader.GetRequiredDateTime("CreatedAtUtc"),
            reader.GetRequiredDateTime("UpdatedAtUtc"));
    }
}

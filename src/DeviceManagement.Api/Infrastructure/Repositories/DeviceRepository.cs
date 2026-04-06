using System.Text;
using System.Text.RegularExpressions;
using DeviceManagement.Api.Domain;
using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace DeviceManagement.Api.Infrastructure.Repositories;

public sealed class DeviceRepository : IDeviceRepository
{
    private static readonly Regex SearchTokenRegex = new(
        @"[\p{L}\p{Nd}]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

    public async Task<IReadOnlyCollection<Device>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var tokens = TokenizeQuery(query);
        if (tokens.Count == 0)
        {
            return await GetAllAsync(cancellationToken);
        }

        var normalizedPhrase = string.Join(' ', tokens);
        var sql = BuildSearchSql(tokens.Count);

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        AddSearchParameters(command, normalizedPhrase, tokens);

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

    public async Task<bool> AssignAsync(int id, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Devices
            SET
                AssignedUserId = @UserId,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE Id = @Id
              AND AssignedUserId IS NULL;
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;
        command.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = userId;

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<bool> UnassignAsync(int id, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Devices
            SET
                AssignedUserId = NULL,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE Id = @Id
              AND AssignedUserId = @UserId;
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;
        command.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = userId;

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

    private static string BuildSearchSql(int tokenCount)
    {
        var scoreParts = new List<string>
        {
            "CASE WHEN LOWER(d.Name) = @SearchPhrase THEN 220 ELSE 0 END",
            "CASE WHEN LOWER(d.Name) LIKE @SearchPhrasePrefix THEN 120 ELSE 0 END",
            "CASE WHEN LOWER(d.Name) LIKE @SearchPhraseContains THEN 80 ELSE 0 END",
            "CASE WHEN LOWER(d.Manufacturer) = @SearchPhrase THEN 160 ELSE 0 END",
            "CASE WHEN LOWER(d.Manufacturer) LIKE @SearchPhrasePrefix THEN 90 ELSE 0 END",
            "CASE WHEN LOWER(d.Manufacturer) LIKE @SearchPhraseContains THEN 60 ELSE 0 END",
            "CASE WHEN LOWER(d.Processor) = @SearchPhrase THEN 140 ELSE 0 END",
            "CASE WHEN LOWER(d.Processor) LIKE @SearchPhraseContains THEN 45 ELSE 0 END"
        };

        for (var index = 0; index < tokenCount; index++)
        {
            scoreParts.Add($"CASE WHEN LOWER(d.Name) LIKE @TokenContains{index} THEN 45 ELSE 0 END");
            scoreParts.Add($"CASE WHEN LOWER(d.Name) LIKE @TokenPrefix{index} THEN 20 ELSE 0 END");
            scoreParts.Add($"CASE WHEN LOWER(d.Manufacturer) LIKE @TokenContains{index} THEN 25 ELSE 0 END");
            scoreParts.Add($"CASE WHEN LOWER(d.Processor) LIKE @TokenContains{index} THEN 20 ELSE 0 END");
            scoreParts.Add($"CASE WHEN CAST(d.RamAmountGb AS NVARCHAR(10)) = @TokenExact{index} THEN 18 ELSE 0 END");
            scoreParts.Add($"CASE WHEN CONCAT(CAST(d.RamAmountGb AS NVARCHAR(10)), 'gb') = @TokenExact{index} THEN 18 ELSE 0 END");
        }

        var scoreExpression = string.Join(
            Environment.NewLine + "                  + ",
            scoreParts);

        return $$"""
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
            CROSS APPLY
            (
                SELECT
                    {{scoreExpression}} AS SearchScore
            ) AS score
            WHERE score.SearchScore > 0
            ORDER BY score.SearchScore DESC, d.Name, d.Manufacturer, d.Id;
            """;
    }

    private static void AddSearchParameters(
        SqlCommand command,
        string normalizedPhrase,
        IReadOnlyList<string> tokens)
    {
        command.Parameters.Add("@SearchPhrase", System.Data.SqlDbType.NVarChar, 250).Value = normalizedPhrase;
        command.Parameters.Add("@SearchPhrasePrefix", System.Data.SqlDbType.NVarChar, 260).Value = normalizedPhrase + "%";
        command.Parameters.Add("@SearchPhraseContains", System.Data.SqlDbType.NVarChar, 270).Value = "%" + normalizedPhrase + "%";

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];

            command.Parameters.Add($"@TokenExact{index}", System.Data.SqlDbType.NVarChar, 50).Value = token;
            command.Parameters.Add($"@TokenPrefix{index}", System.Data.SqlDbType.NVarChar, 60).Value = token + "%";
            command.Parameters.Add($"@TokenContains{index}", System.Data.SqlDbType.NVarChar, 70).Value = "%" + token + "%";
        }
    }

    private static IReadOnlyList<string> TokenizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<string>();
        }

        return SearchTokenRegex
            .Matches(query.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(8)
            .ToArray();
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

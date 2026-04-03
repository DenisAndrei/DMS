using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace DeviceManagement.Api.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public UserRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Id,
                Name,
                Role,
                Location,
                CreatedAtUtc,
                UpdatedAtUtc
            FROM dbo.Users
            ORDER BY Name;
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var users = new List<User>();
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(MapUser(reader));
        }

        return users;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Id,
                Name,
                Role,
                Location,
                CreatedAtUtc,
                UpdatedAtUtc
            FROM dbo.Users
            WHERE Id = @Id;
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapUser(reader);
    }

    public async Task<int> CreateAsync(UserUpsertModel user, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO dbo.Users
            (
                Name,
                Role,
                Location
            )
            VALUES (@Name, @Role, @Location);

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        AddUpsertParameters(command, user);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateAsync(int id, UserUpsertModel user, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.Users
            SET
                Name = @Name,
                Role = @Role,
                Location = @Location,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE Id = @Id;
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        AddUpsertParameters(command, user);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM dbo.Users WHERE Id = @Id;";

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE Id = @Id;";

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task<bool> HasAssignedDevicesAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Devices WHERE AssignedUserId = @Id;";

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Id", System.Data.SqlDbType.Int).Value = id;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static void AddUpsertParameters(SqlCommand command, UserUpsertModel user)
    {
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 120).Value = user.Name;
        command.Parameters.Add("@Role", System.Data.SqlDbType.NVarChar, 120).Value = user.Role;
        command.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 120).Value = user.Location;
    }

    private static User MapUser(SqlDataReader reader) =>
        new(
            reader.GetRequiredInt32("Id"),
            reader.GetRequiredString("Name"),
            reader.GetRequiredString("Role"),
            reader.GetRequiredString("Location"),
            reader.GetRequiredDateTime("CreatedAtUtc"),
            reader.GetRequiredDateTime("UpdatedAtUtc"));
}

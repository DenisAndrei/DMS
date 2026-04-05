using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace DeviceManagement.Api.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public AccountRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<AuthAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                a.Id AS AccountId,
                a.UserId,
                a.Email,
                a.PasswordHash,
                a.PasswordSalt,
                u.Name,
                u.Role,
                u.Location
            FROM dbo.Accounts AS a
            INNER JOIN dbo.Users AS u
                ON u.Id = a.UserId
            WHERE a.Email = @Email;
            """;

        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar, 256).Value = email;

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapAuthAccount(reader);
    }

    public async Task<AuthAccount> CreateAsync(
        string email,
        string passwordHash,
        string passwordSalt,
        UserUpsertModel userProfile,
        CancellationToken cancellationToken)
    {
        using var connection = await _dbConnectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string insertUserSql = """
                INSERT INTO dbo.Users
                (
                    Name,
                    Role,
                    Location
                )
                VALUES (@Name, @Role, @Location);

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            using var insertUserCommand = new SqlCommand(insertUserSql, connection, (SqlTransaction)transaction);
            insertUserCommand.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 120).Value = userProfile.Name;
            insertUserCommand.Parameters.Add("@Role", System.Data.SqlDbType.NVarChar, 120).Value = userProfile.Role;
            insertUserCommand.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 120).Value = userProfile.Location;

            var createdUserId = Convert.ToInt32(await insertUserCommand.ExecuteScalarAsync(cancellationToken));

            const string insertAccountSql = """
                INSERT INTO dbo.Accounts
                (
                    UserId,
                    Email,
                    PasswordHash,
                    PasswordSalt
                )
                VALUES (@UserId, @Email, @PasswordHash, @PasswordSalt);

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            using var insertAccountCommand = new SqlCommand(insertAccountSql, connection, (SqlTransaction)transaction);
            insertAccountCommand.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = createdUserId;
            insertAccountCommand.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar, 256).Value = email;
            insertAccountCommand.Parameters.Add("@PasswordHash", System.Data.SqlDbType.NVarChar, 128).Value = passwordHash;
            insertAccountCommand.Parameters.Add("@PasswordSalt", System.Data.SqlDbType.NVarChar, 128).Value = passwordSalt;

            var createdAccountId = Convert.ToInt32(await insertAccountCommand.ExecuteScalarAsync(cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            return new AuthAccount(
                createdAccountId,
                createdUserId,
                email,
                passwordHash,
                passwordSalt,
                userProfile.Name,
                userProfile.Role,
                userProfile.Location);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static AuthAccount MapAuthAccount(SqlDataReader reader) =>
        new(
            reader.GetRequiredInt32("AccountId"),
            reader.GetRequiredInt32("UserId"),
            reader.GetRequiredString("Email"),
            reader.GetRequiredString("PasswordHash"),
            reader.GetRequiredString("PasswordSalt"),
            reader.GetRequiredString("Name"),
            reader.GetRequiredString("Role"),
            reader.GetRequiredString("Location"));
}

using Microsoft.Data.SqlClient;

namespace DeviceManagement.Api.Infrastructure.Data;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DeviceManagement")
            ?? throw new InvalidOperationException("Connection string 'DeviceManagement' is missing.");
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        // Open a new SQL connection for each repository operation.
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

using Microsoft.Data.SqlClient;

namespace DeviceManagement.Api.Infrastructure.Data;

public interface IDbConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}

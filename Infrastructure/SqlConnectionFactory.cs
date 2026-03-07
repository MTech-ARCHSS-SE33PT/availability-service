using Microsoft.Data.SqlClient;

namespace AvailabilityService.Infrastructure;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}

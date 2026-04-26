using AvailabilityService.Infrastructure;
using Xunit;

namespace AvailabilityService.Tests.Infrastructure;

public sealed class SqlConnectionFactoryTests
{
    [Fact]
    public void CreateConnection_UsesConfiguredConnectionString()
    {
        const string connectionString = "Server=localhost;Database=QueueX;User Id=test;Password=test;TrustServerCertificate=True";
        var factory = new SqlConnectionFactory(connectionString);

        using var connection = factory.CreateConnection();

        Assert.Equal(connectionString, connection.ConnectionString);
    }
}

using Microsoft.Data.SqlClient;

namespace AvailabilityService.Infrastructure;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}

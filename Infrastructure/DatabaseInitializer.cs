using System.Data.Common;

namespace AvailabilityService.Infrastructure;

public sealed class DatabaseInitializer
{
    private readonly Func<DbConnection> _createConnection;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _createConnection = connectionFactory.CreateConnection;
    }

    public DatabaseInitializer(Func<DbConnection> createConnection)
    {
        _createConnection = createConnection;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.AvailabilityRules', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AvailabilityRules
                (
                    RuleId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    TenantId UNIQUEIDENTIFIER NOT NULL,
                    ServiceId UNIQUEIDENTIFIER NOT NULL,
                    DayOfWeek INT NOT NULL,
                    OperatingStartTime TIME NOT NULL,
                    OperatingEndTime TIME NOT NULL,
                    IsActive BIT NOT NULL,
                    CreatedAt DATETIMEOFFSET NOT NULL
                );

                CREATE INDEX IX_AvailabilityRules_Tenant_Service_Day
                    ON dbo.AvailabilityRules (TenantId, ServiceId, DayOfWeek);
            END;

            IF OBJECT_ID(N'dbo.AvailabilityExceptions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AvailabilityExceptions
                (
                    RuleExceptionId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    TenantId UNIQUEIDENTIFIER NOT NULL,
                    ServiceId UNIQUEIDENTIFIER NOT NULL,
                    ExceptionDate DATE NOT NULL,
                    Reason NVARCHAR(100) NULL,
                    IsActive BIT NOT NULL,
                    CreatedAt DATETIMEOFFSET NOT NULL
                );

                CREATE INDEX IX_AvailabilityExceptions_Tenant_Service_Date
                    ON dbo.AvailabilityExceptions (TenantId, ServiceId, ExceptionDate);
            END;
            """;

        await using var connection = _createConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct);
    }
}

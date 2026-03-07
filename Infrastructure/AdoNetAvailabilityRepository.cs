using AvailabilityService.Models;
using Microsoft.Data.SqlClient;

namespace AvailabilityService.Infrastructure;

public sealed class AdoNetAvailabilityRepository : IAvailabilityRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AdoNetAvailabilityRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> HasAnyRulesAsync(CancellationToken ct)
    {
        const string sql = "SELECT TOP 1 1 FROM dbo.AvailabilityRules";
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(ct);
        return result is not null;
    }

    public async Task AddRuleAsync(AvailabilityRule rule, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.AvailabilityRules
            (RuleId, TenantId, ServiceId, DayOfWeek, OperatingStartTime, OperatingEndTime, IsActive, CreatedAt)
            VALUES
            (@RuleId, @TenantId, @ServiceId, @DayOfWeek, @OperatingStartTime, @OperatingEndTime, @IsActive, @CreatedAt)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        AddRuleParameters(command, rule);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<AvailabilityRule>> GetRulesAsync(Guid tenantId, Guid serviceId, CancellationToken ct)
    {
        const string sql = """
            SELECT RuleId, TenantId, ServiceId, DayOfWeek, OperatingStartTime, OperatingEndTime, IsActive, CreatedAt
            FROM dbo.AvailabilityRules
            WHERE TenantId = @TenantId AND ServiceId = @ServiceId
            ORDER BY DayOfWeek, OperatingStartTime
            """;

        return await QueryRulesAsync(sql, ct, ("@TenantId", tenantId), ("@ServiceId", serviceId));
    }

    public async Task<AvailabilityRule?> GetRuleAsync(Guid ruleId, CancellationToken ct)
    {
        const string sql = """
            SELECT RuleId, TenantId, ServiceId, DayOfWeek, OperatingStartTime, OperatingEndTime, IsActive, CreatedAt
            FROM dbo.AvailabilityRules
            WHERE RuleId = @RuleId
            """;

        var rules = await QueryRulesAsync(sql, ct, ("@RuleId", ruleId));
        return rules.FirstOrDefault();
    }

    public async Task UpdateRuleAsync(AvailabilityRule rule, CancellationToken ct)
    {
        const string sql = """
            UPDATE dbo.AvailabilityRules
            SET OperatingStartTime = @OperatingStartTime,
                OperatingEndTime = @OperatingEndTime,
                IsActive = @IsActive
            WHERE RuleId = @RuleId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RuleId", rule.RuleId);
        command.Parameters.AddWithValue("@OperatingStartTime", rule.OperatingStartTime.ToTimeSpan());
        command.Parameters.AddWithValue("@OperatingEndTime", rule.OperatingEndTime.ToTimeSpan());
        command.Parameters.AddWithValue("@IsActive", rule.IsActive);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task AddExceptionAsync(AvailabilityException item, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.AvailabilityExceptions
            (RuleExceptionId, TenantId, ServiceId, ExceptionDate, Reason, IsActive, CreatedAt)
            VALUES
            (@RuleExceptionId, @TenantId, @ServiceId, @ExceptionDate, @Reason, @IsActive, @CreatedAt)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        AddExceptionParameters(command, item);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<AvailabilityException>> GetExceptionsAsync(Guid tenantId, Guid serviceId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        const string sql = """
            SELECT RuleExceptionId, TenantId, ServiceId, ExceptionDate, Reason, IsActive, CreatedAt
            FROM dbo.AvailabilityExceptions
            WHERE TenantId = @TenantId
              AND ServiceId = @ServiceId
              AND ExceptionDate >= @FromDate
              AND ExceptionDate <= @ToDate
            ORDER BY ExceptionDate
            """;

        return await QueryExceptionsAsync(sql, ct,
            ("@TenantId", tenantId),
            ("@ServiceId", serviceId),
            ("@FromDate", from.ToDateTime(TimeOnly.MinValue)),
            ("@ToDate", to.ToDateTime(TimeOnly.MinValue)));
    }

    public async Task<AvailabilityException?> GetActiveExceptionAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct)
    {
        const string sql = """
            SELECT RuleExceptionId, TenantId, ServiceId, ExceptionDate, Reason, IsActive, CreatedAt
            FROM dbo.AvailabilityExceptions
            WHERE TenantId = @TenantId
              AND ServiceId = @ServiceId
              AND ExceptionDate = @ExceptionDate
              AND IsActive = 1
            """;

        var items = await QueryExceptionsAsync(sql, ct,
            ("@TenantId", tenantId),
            ("@ServiceId", serviceId),
            ("@ExceptionDate", date.ToDateTime(TimeOnly.MinValue)));
        return items.FirstOrDefault();
    }

    public async Task<bool> DeleteExceptionAsync(Guid ruleExceptionId, CancellationToken ct)
    {
        const string sql = "DELETE FROM dbo.AvailabilityExceptions WHERE RuleExceptionId = @RuleExceptionId";
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RuleExceptionId", ruleExceptionId);
        var rows = await command.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }

    public async Task<IReadOnlyList<AvailabilityRule>> GetActiveRulesByDayAsync(Guid tenantId, Guid serviceId, int dayOfWeek, CancellationToken ct)
    {
        const string sql = """
            SELECT RuleId, TenantId, ServiceId, DayOfWeek, OperatingStartTime, OperatingEndTime, IsActive, CreatedAt
            FROM dbo.AvailabilityRules
            WHERE TenantId = @TenantId
              AND ServiceId = @ServiceId
              AND DayOfWeek = @DayOfWeek
              AND IsActive = 1
            ORDER BY OperatingStartTime, OperatingEndTime
            """;

        return await QueryRulesAsync(sql, ct,
            ("@TenantId", tenantId),
            ("@ServiceId", serviceId),
            ("@DayOfWeek", dayOfWeek));
    }

    private async Task<List<AvailabilityRule>> QueryRulesAsync(string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        var results = new List<AvailabilityRule>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new AvailabilityRule
            {
                RuleId = reader.GetGuid(0),
                TenantId = reader.GetGuid(1),
                ServiceId = reader.GetGuid(2),
                DayOfWeek = reader.GetInt32(3),
                OperatingStartTime = TimeOnly.FromTimeSpan(reader.GetTimeSpan(4)),
                OperatingEndTime = TimeOnly.FromTimeSpan(reader.GetTimeSpan(5)),
                IsActive = reader.GetBoolean(6),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(7)
            });
        }

        return results;
    }

    private async Task<List<AvailabilityException>> QueryExceptionsAsync(string sql, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        var results = new List<AvailabilityException>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new AvailabilityException
            {
                RuleExceptionId = reader.GetGuid(0),
                TenantId = reader.GetGuid(1),
                ServiceId = reader.GetGuid(2),
                ExceptionDate = DateOnly.FromDateTime(reader.GetDateTime(3)),
                Reason = reader.IsDBNull(4) ? null : reader.GetString(4),
                IsActive = reader.GetBoolean(5),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(6)
            });
        }

        return results;
    }

    private static void AddRuleParameters(SqlCommand command, AvailabilityRule rule)
    {
        command.Parameters.AddWithValue("@RuleId", rule.RuleId);
        command.Parameters.AddWithValue("@TenantId", rule.TenantId);
        command.Parameters.AddWithValue("@ServiceId", rule.ServiceId);
        command.Parameters.AddWithValue("@DayOfWeek", rule.DayOfWeek);
        command.Parameters.AddWithValue("@OperatingStartTime", rule.OperatingStartTime.ToTimeSpan());
        command.Parameters.AddWithValue("@OperatingEndTime", rule.OperatingEndTime.ToTimeSpan());
        command.Parameters.AddWithValue("@IsActive", rule.IsActive);
        command.Parameters.AddWithValue("@CreatedAt", rule.CreatedAt);
    }

    private static void AddExceptionParameters(SqlCommand command, AvailabilityException item)
    {
        command.Parameters.AddWithValue("@RuleExceptionId", item.RuleExceptionId);
        command.Parameters.AddWithValue("@TenantId", item.TenantId);
        command.Parameters.AddWithValue("@ServiceId", item.ServiceId);
        command.Parameters.AddWithValue("@ExceptionDate", item.ExceptionDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@Reason", (object?)item.Reason ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", item.IsActive);
        command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt);
    }
}

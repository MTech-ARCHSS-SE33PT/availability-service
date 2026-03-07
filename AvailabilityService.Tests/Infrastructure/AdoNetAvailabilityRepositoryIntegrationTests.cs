using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Xunit;

namespace AvailabilityService.Tests.Infrastructure;

public sealed class AdoNetAvailabilityRepositoryIntegrationTests
{
    [Fact]
    public async Task AddRuleAndException_RoundTrips_WhenSqlConnectionProvided()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var factory = new SqlConnectionFactory(connectionString);
        var initializer = new DatabaseInitializer(factory);
        await initializer.InitializeAsync(CancellationToken.None);

        var repository = new AdoNetAvailabilityRepository(factory);
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var rule = new AvailabilityRule
        {
            RuleId = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            DayOfWeek = 1,
            OperatingStartTime = new TimeOnly(9, 0),
            OperatingEndTime = new TimeOnly(10, 0),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await repository.AddRuleAsync(rule, CancellationToken.None);

        var exception = new AvailabilityException
        {
            RuleExceptionId = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            ExceptionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Reason = "integration-test",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await repository.AddExceptionAsync(exception, CancellationToken.None);

        var rules = await repository.GetRulesAsync(tenantId, serviceId, CancellationToken.None);
        var activeException = await repository.GetActiveExceptionAsync(tenantId, serviceId, exception.ExceptionDate, CancellationToken.None);

        Assert.Contains(rules, r => r.RuleId == rule.RuleId);
        Assert.NotNull(activeException);
        Assert.Equal(exception.RuleExceptionId, activeException!.RuleExceptionId);
    }
}

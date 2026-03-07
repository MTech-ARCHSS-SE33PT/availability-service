using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Xunit;

namespace AvailabilityService.Tests.Infrastructure;

public sealed class InMemoryAvailabilityRepositoryTests
{
    [Fact]
    public async Task RuleLifecycle_WorksAsExpected()
    {
        var repository = new InMemoryAvailabilityRepository(new InMemoryAvailabilityStore());
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

        Assert.False(await repository.HasAnyRulesAsync(CancellationToken.None));
        await repository.AddRuleAsync(rule, CancellationToken.None);
        Assert.True(await repository.HasAnyRulesAsync(CancellationToken.None));

        var loaded = await repository.GetRuleAsync(rule.RuleId, CancellationToken.None);
        Assert.NotNull(loaded);

        loaded!.IsActive = false;
        await repository.UpdateRuleAsync(loaded, CancellationToken.None);

        var activeByDay = await repository.GetActiveRulesByDayAsync(tenantId, serviceId, 1, CancellationToken.None);
        Assert.Empty(activeByDay);
    }

    [Fact]
    public async Task ExceptionLifecycle_WorksAsExpected()
    {
        var repository = new InMemoryAvailabilityRepository(new InMemoryAvailabilityStore());
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 15);
        var exception = new AvailabilityException
        {
            RuleExceptionId = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            ExceptionDate = date,
            Reason = "Holiday",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await repository.AddExceptionAsync(exception, CancellationToken.None);

        var active = await repository.GetActiveExceptionAsync(tenantId, serviceId, date, CancellationToken.None);
        Assert.NotNull(active);

        var range = await repository.GetExceptionsAsync(tenantId, serviceId, date.AddDays(-1), date.AddDays(1), CancellationToken.None);
        Assert.Single(range);

        var deleted = await repository.DeleteExceptionAsync(exception.RuleExceptionId, CancellationToken.None);
        Assert.True(deleted);

        var deletedAgain = await repository.DeleteExceptionAsync(exception.RuleExceptionId, CancellationToken.None);
        Assert.False(deletedAgain);
    }
}

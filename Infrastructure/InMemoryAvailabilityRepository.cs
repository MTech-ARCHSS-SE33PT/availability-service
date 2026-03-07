using AvailabilityService.Models;

namespace AvailabilityService.Infrastructure;

public sealed class InMemoryAvailabilityRepository : IAvailabilityRepository
{
    private readonly InMemoryAvailabilityStore _store;

    public InMemoryAvailabilityRepository(InMemoryAvailabilityStore store)
    {
        _store = store;
    }

    public Task<bool> HasAnyRulesAsync(CancellationToken ct)
        => Task.FromResult(!_store.Rules.IsEmpty);

    public Task AddRuleAsync(AvailabilityRule rule, CancellationToken ct)
    {
        _store.Rules[rule.RuleId] = rule;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AvailabilityRule>> GetRulesAsync(Guid tenantId, Guid serviceId, CancellationToken ct)
    {
        var rules = _store.Rules.Values
            .Where(r => r.TenantId == tenantId && r.ServiceId == serviceId)
            .OrderBy(r => r.DayOfWeek)
            .ThenBy(r => r.OperatingStartTime)
            .ToList();

        return Task.FromResult<IReadOnlyList<AvailabilityRule>>(rules);
    }

    public Task<AvailabilityRule?> GetRuleAsync(Guid ruleId, CancellationToken ct)
    {
        _store.Rules.TryGetValue(ruleId, out var rule);
        return Task.FromResult(rule);
    }

    public Task UpdateRuleAsync(AvailabilityRule rule, CancellationToken ct)
    {
        _store.Rules[rule.RuleId] = rule;
        return Task.CompletedTask;
    }

    public Task AddExceptionAsync(AvailabilityException item, CancellationToken ct)
    {
        _store.Exceptions[item.RuleExceptionId] = item;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AvailabilityException>> GetExceptionsAsync(Guid tenantId, Guid serviceId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var results = _store.Exceptions.Values
            .Where(e => e.TenantId == tenantId &&
                        e.ServiceId == serviceId &&
                        e.ExceptionDate >= from &&
                        e.ExceptionDate <= to)
            .OrderBy(e => e.ExceptionDate)
            .ToList();

        return Task.FromResult<IReadOnlyList<AvailabilityException>>(results);
    }

    public Task<AvailabilityException?> GetActiveExceptionAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct)
    {
        var item = _store.Exceptions.Values.FirstOrDefault(e =>
            e.IsActive &&
            e.TenantId == tenantId &&
            e.ServiceId == serviceId &&
            e.ExceptionDate == date);

        return Task.FromResult(item);
    }

    public Task<bool> DeleteExceptionAsync(Guid ruleExceptionId, CancellationToken ct)
        => Task.FromResult(_store.Exceptions.TryRemove(ruleExceptionId, out _));

    public Task<IReadOnlyList<AvailabilityRule>> GetActiveRulesByDayAsync(Guid tenantId, Guid serviceId, int dayOfWeek, CancellationToken ct)
    {
        var rules = _store.Rules.Values
            .Where(r => r.IsActive &&
                        r.TenantId == tenantId &&
                        r.ServiceId == serviceId &&
                        r.DayOfWeek == dayOfWeek)
            .OrderBy(r => r.OperatingStartTime)
            .ThenBy(r => r.OperatingEndTime)
            .ToList();

        return Task.FromResult<IReadOnlyList<AvailabilityRule>>(rules);
    }
}

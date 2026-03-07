using AvailabilityService.Models;

namespace AvailabilityService.Infrastructure;

public interface IAvailabilityRepository
{
    Task<bool> HasAnyRulesAsync(CancellationToken ct);
    Task AddRuleAsync(AvailabilityRule rule, CancellationToken ct);
    Task<IReadOnlyList<AvailabilityRule>> GetRulesAsync(Guid tenantId, Guid serviceId, CancellationToken ct);
    Task<AvailabilityRule?> GetRuleAsync(Guid ruleId, CancellationToken ct);
    Task UpdateRuleAsync(AvailabilityRule rule, CancellationToken ct);

    Task AddExceptionAsync(AvailabilityException item, CancellationToken ct);
    Task<IReadOnlyList<AvailabilityException>> GetExceptionsAsync(Guid tenantId, Guid serviceId, DateOnly from, DateOnly to, CancellationToken ct);
    Task<AvailabilityException?> GetActiveExceptionAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct);
    Task<bool> DeleteExceptionAsync(Guid ruleExceptionId, CancellationToken ct);

    Task<IReadOnlyList<AvailabilityRule>> GetActiveRulesByDayAsync(Guid tenantId, Guid serviceId, int dayOfWeek, CancellationToken ct);
}

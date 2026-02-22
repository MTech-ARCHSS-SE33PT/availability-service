using AvailabilityService.Models;

namespace AvailabilityService.Infrastructure;

public static class AvailabilitySeedData
{
    private static readonly Guid SeedTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SeedServiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static void Seed(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryAvailabilityStore>();

        if (!store.Rules.IsEmpty)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        for (var day = 1; day <= 5; day++)
        {
            AddRule(store, SeedTenantId, SeedServiceId, day, new TimeOnly(9, 0), new TimeOnly(12, 0), now);
            AddRule(store, SeedTenantId, SeedServiceId, day, new TimeOnly(13, 0), new TimeOnly(18, 0), now);
        }
    }

    private static void AddRule(
        InMemoryAvailabilityStore store,
        Guid tenantId,
        Guid serviceId,
        int dayOfWeek,
        TimeOnly start,
        TimeOnly end,
        DateTimeOffset createdAt)
    {
        var rule = new AvailabilityRule
        {
            RuleId = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            DayOfWeek = dayOfWeek,
            OperatingStartTime = start,
            OperatingEndTime = end,
            IsActive = true,
            CreatedAt = createdAt
        };

        store.Rules[rule.RuleId] = rule;
    }
}


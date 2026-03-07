using AvailabilityService.Models;

namespace AvailabilityService.Infrastructure;

public static class AvailabilitySeedData
{
    private static readonly Guid SeedTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SeedServiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAvailabilityRepository>();

        if (await repository.HasAnyRulesAsync(ct))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        for (var day = 1; day <= 5; day++)
        {
            await AddRuleAsync(repository, SeedTenantId, SeedServiceId, day, new TimeOnly(9, 0), new TimeOnly(12, 0), now, ct);
            await AddRuleAsync(repository, SeedTenantId, SeedServiceId, day, new TimeOnly(13, 0), new TimeOnly(18, 0), now, ct);
        }
    }

    private static async Task AddRuleAsync(
        IAvailabilityRepository repository,
        Guid tenantId,
        Guid serviceId,
        int dayOfWeek,
        TimeOnly start,
        TimeOnly end,
        DateTimeOffset createdAt,
        CancellationToken ct)
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

        await repository.AddRuleAsync(rule, ct);
    }
}

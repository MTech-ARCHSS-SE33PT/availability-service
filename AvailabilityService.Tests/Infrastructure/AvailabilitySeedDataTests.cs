using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AvailabilityService.Tests.Infrastructure;

public sealed class AvailabilitySeedDataTests
{
    [Fact]
    public async Task SeedAsync_WhenRulesExist_DoesNothing()
    {
        var repository = new FakeRepository { HasRules = true };
        var services = BuildServices(repository);

        await AvailabilitySeedData.SeedAsync(services, CancellationToken.None);

        Assert.Empty(repository.AddedRules);
    }

    [Fact]
    public async Task SeedAsync_WhenNoRules_AddsTenWeekdayRules()
    {
        var repository = new FakeRepository { HasRules = false };
        var services = BuildServices(repository);

        await AvailabilitySeedData.SeedAsync(services, CancellationToken.None);

        Assert.Equal(10, repository.AddedRules.Count);
        Assert.All(repository.AddedRules, r => Assert.True(r.IsActive));
        Assert.All(repository.AddedRules, r => Assert.InRange(r.DayOfWeek, 1, 5));
    }

    private static IServiceProvider BuildServices(IAvailabilityRepository repository)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => repository);
        return services.BuildServiceProvider();
    }

    private sealed class FakeRepository : IAvailabilityRepository
    {
        public bool HasRules { get; set; }
        public List<AvailabilityRule> AddedRules { get; } = new();

        public Task<bool> HasAnyRulesAsync(CancellationToken ct) => Task.FromResult(HasRules);
        public Task AddRuleAsync(AvailabilityRule rule, CancellationToken ct) { AddedRules.Add(rule); return Task.CompletedTask; }
        public Task<IReadOnlyList<AvailabilityRule>> GetRulesAsync(Guid tenantId, Guid serviceId, CancellationToken ct) => Task.FromResult<IReadOnlyList<AvailabilityRule>>(Array.Empty<AvailabilityRule>());
        public Task<AvailabilityRule?> GetRuleAsync(Guid ruleId, CancellationToken ct) => Task.FromResult<AvailabilityRule?>(null);
        public Task UpdateRuleAsync(AvailabilityRule rule, CancellationToken ct) => Task.CompletedTask;
        public Task AddExceptionAsync(AvailabilityException item, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AvailabilityException>> GetExceptionsAsync(Guid tenantId, Guid serviceId, DateOnly from, DateOnly to, CancellationToken ct) => Task.FromResult<IReadOnlyList<AvailabilityException>>(Array.Empty<AvailabilityException>());
        public Task<AvailabilityException?> GetActiveExceptionAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct) => Task.FromResult<AvailabilityException?>(null);
        public Task<bool> DeleteExceptionAsync(Guid ruleExceptionId, CancellationToken ct) => Task.FromResult(false);
        public Task<IReadOnlyList<AvailabilityRule>> GetActiveRulesByDayAsync(Guid tenantId, Guid serviceId, int dayOfWeek, CancellationToken ct) => Task.FromResult<IReadOnlyList<AvailabilityRule>>(Array.Empty<AvailabilityRule>());
    }
}

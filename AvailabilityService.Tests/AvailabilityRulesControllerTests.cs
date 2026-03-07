using AvailabilityService.Controllers;
using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AvailabilityService.Tests;

public sealed class AvailabilityRulesControllerTests
{
    [Fact]
    public async Task Create_WithInvalidDayOfWeek_ReturnsBadRequest()
    {
        var controller = new AvailabilityRulesController(new FakeAvailabilityRepository());

        var result = await controller.Create(new CreateRuleRequest
        {
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            DayOfWeek = 0,
            OperatingStartTime = "09:00",
            OperatingEndTime = "10:00"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        var repository = new FakeAvailabilityRepository();
        var controller = new AvailabilityRulesController(repository);

        var result = await controller.Create(new CreateRuleRequest
        {
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            DayOfWeek = 1,
            OperatingStartTime = "09:00",
            OperatingEndTime = "10:00"
        }, CancellationToken.None);

        Assert.IsType<CreatedResult>(result);
        Assert.Single(repository.Rules);
    }

    [Fact]
    public async Task Create_WithInvalidStartTime_ReturnsBadRequest()
    {
        var controller = new AvailabilityRulesController(new FakeAvailabilityRepository());
        var result = await controller.Create(new CreateRuleRequest
        {
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            DayOfWeek = 1,
            OperatingStartTime = "invalid",
            OperatingEndTime = "10:00"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithInvalidEndTime_ReturnsBadRequest()
    {
        var controller = new AvailabilityRulesController(new FakeAvailabilityRepository());
        var result = await controller.Create(new CreateRuleRequest
        {
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            DayOfWeek = 1,
            OperatingStartTime = "09:00",
            OperatingEndTime = "invalid"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenEndBeforeStart_ReturnsBadRequest()
    {
        var controller = new AvailabilityRulesController(new FakeAvailabilityRepository());
        var result = await controller.Create(new CreateRuleRequest
        {
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            DayOfWeek = 1,
            OperatingStartTime = "10:00",
            OperatingEndTime = "09:00"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsRules()
    {
        var repository = new FakeAvailabilityRepository();
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        repository.Rules.Add(new AvailabilityRule
        {
            RuleId = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            DayOfWeek = 1,
            OperatingStartTime = new TimeOnly(9, 0),
            OperatingEndTime = new TimeOnly(10, 0),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var controller = new AvailabilityRulesController(repository);

        var result = await controller.Get(tenantId, serviceId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var rules = Assert.IsAssignableFrom<IReadOnlyList<AvailabilityRule>>(ok.Value);
        Assert.Single(rules);
    }

    [Fact]
    public async Task Patch_WhenRuleMissing_ReturnsNotFound()
    {
        var controller = new AvailabilityRulesController(new FakeAvailabilityRepository());
        var result = await controller.Patch(Guid.NewGuid(), new UpdateRuleRequest(), CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Patch_WithInvalidStartTime_ReturnsBadRequest()
    {
        var repository = new FakeAvailabilityRepository();
        var rule = AddRule(repository);
        var controller = new AvailabilityRulesController(repository);

        var result = await controller.Patch(rule.RuleId, new UpdateRuleRequest
        {
            OperatingStartTime = "bad"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Patch_WithInvalidEndTime_ReturnsBadRequest()
    {
        var repository = new FakeAvailabilityRepository();
        var rule = AddRule(repository);
        var controller = new AvailabilityRulesController(repository);

        var result = await controller.Patch(rule.RuleId, new UpdateRuleRequest
        {
            OperatingEndTime = "bad"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Patch_WhenEndBeforeStart_ReturnsBadRequest()
    {
        var repository = new FakeAvailabilityRepository();
        var rule = AddRule(repository);
        var controller = new AvailabilityRulesController(repository);

        var result = await controller.Patch(rule.RuleId, new UpdateRuleRequest
        {
            OperatingStartTime = "10:00",
            OperatingEndTime = "09:00"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Patch_WithValidValues_ReturnsUpdatedRule()
    {
        var repository = new FakeAvailabilityRepository();
        var rule = AddRule(repository);
        var controller = new AvailabilityRulesController(repository);

        var result = await controller.Patch(rule.RuleId, new UpdateRuleRequest
        {
            OperatingStartTime = "08:00",
            OperatingEndTime = "09:00",
            IsActive = false
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<AvailabilityRule>(ok.Value);
        Assert.Equal(new TimeOnly(8, 0), updated.OperatingStartTime);
        Assert.Equal(new TimeOnly(9, 0), updated.OperatingEndTime);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Deactivate_WhenRuleMissing_ReturnsNotFound()
    {
        var controller = new AvailabilityRulesController(new FakeAvailabilityRepository());
        var result = await controller.Deactivate(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Deactivate_WhenRuleExists_ReturnsOkAndSetsInactive()
    {
        var repository = new FakeAvailabilityRepository();
        var rule = AddRule(repository);
        var controller = new AvailabilityRulesController(repository);

        var result = await controller.Deactivate(rule.RuleId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<AvailabilityRule>(ok.Value);
        Assert.False(updated.IsActive);
    }

    private static AvailabilityRule AddRule(FakeAvailabilityRepository repository)
    {
        var rule = new AvailabilityRule
        {
            RuleId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            DayOfWeek = 1,
            OperatingStartTime = new TimeOnly(9, 0),
            OperatingEndTime = new TimeOnly(10, 0),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        repository.Rules.Add(rule);
        return rule;
    }

    private sealed class FakeAvailabilityRepository : IAvailabilityRepository
    {
        public List<AvailabilityRule> Rules { get; } = new();

        public Task<bool> HasAnyRulesAsync(CancellationToken ct) => Task.FromResult(Rules.Count > 0);
        public Task AddRuleAsync(AvailabilityRule rule, CancellationToken ct)
        {
            Rules.Add(rule);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AvailabilityRule>> GetRulesAsync(Guid tenantId, Guid serviceId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AvailabilityRule>>(Rules.Where(r => r.TenantId == tenantId && r.ServiceId == serviceId).ToList());

        public Task<AvailabilityRule?> GetRuleAsync(Guid ruleId, CancellationToken ct)
            => Task.FromResult(Rules.FirstOrDefault(r => r.RuleId == ruleId));

        public Task UpdateRuleAsync(AvailabilityRule rule, CancellationToken ct) => Task.CompletedTask;
        public Task AddExceptionAsync(AvailabilityException item, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AvailabilityException>> GetExceptionsAsync(Guid tenantId, Guid serviceId, DateOnly from, DateOnly to, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AvailabilityException>>(Array.Empty<AvailabilityException>());

        public Task<AvailabilityException?> GetActiveExceptionAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct)
            => Task.FromResult<AvailabilityException?>(null);

        public Task<bool> DeleteExceptionAsync(Guid ruleExceptionId, CancellationToken ct) => Task.FromResult(false);

        public Task<IReadOnlyList<AvailabilityRule>> GetActiveRulesByDayAsync(Guid tenantId, Guid serviceId, int dayOfWeek, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AvailabilityRule>>(Rules.Where(r => r.IsActive && r.DayOfWeek == dayOfWeek).ToList());
    }
}

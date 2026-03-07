using AvailabilityService.Controllers;
using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AvailabilityService.Tests;

public sealed class AvailabilityExceptionsControllerTests
{
    [Fact]
    public async Task Create_WithInvalidDate_ReturnsBadRequest()
    {
        var controller = new AvailabilityExceptionsController(new FakeAvailabilityRepository());

        var result = await controller.Create(new CreateExceptionRequest
        {
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            ExceptionDate = "not-a-date",
            Reason = "x"
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        var repository = new FakeAvailabilityRepository();
        var controller = new AvailabilityExceptionsController(repository);

        var result = await controller.Create(new CreateExceptionRequest
        {
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            ExceptionDate = "2026-03-15",
            Reason = "Holiday"
        }, CancellationToken.None);

        Assert.IsType<CreatedResult>(result);
        Assert.Single(repository.Exceptions);
    }

    [Fact]
    public async Task Create_WithTooLongReason_ReturnsBadRequest()
    {
        var controller = new AvailabilityExceptionsController(new FakeAvailabilityRepository());
        var result = await controller.Create(new CreateExceptionRequest
        {
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            ExceptionDate = "2026-03-15",
            Reason = new string('a', 101)
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_WithInvalidFrom_ReturnsBadRequest()
    {
        var controller = new AvailabilityExceptionsController(new FakeAvailabilityRepository());
        var result = await controller.Get(Guid.NewGuid(), Guid.NewGuid(), "bad", "2026-03-16", CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_WithInvalidTo_ReturnsBadRequest()
    {
        var controller = new AvailabilityExceptionsController(new FakeAvailabilityRepository());
        var result = await controller.Get(Guid.NewGuid(), Guid.NewGuid(), "2026-03-15", "bad", CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_WithToBeforeFrom_ReturnsBadRequest()
    {
        var controller = new AvailabilityExceptionsController(new FakeAvailabilityRepository());
        var result = await controller.Get(Guid.NewGuid(), Guid.NewGuid(), "2026-03-16", "2026-03-15", CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_WithValidRange_ReturnsOk()
    {
        var repository = new FakeAvailabilityRepository();
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        repository.Exceptions.Add(new AvailabilityException
        {
            RuleExceptionId = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            ExceptionDate = new DateOnly(2026, 3, 15),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var controller = new AvailabilityExceptionsController(repository);

        var result = await controller.Get(tenantId, serviceId, "2026-03-15", "2026-03-16", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<AvailabilityException>>(ok.Value);
        Assert.Single(payload);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        var controller = new AvailabilityExceptionsController(new FakeAvailabilityRepository());
        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenExists_ReturnsNoContent()
    {
        var repository = new FakeAvailabilityRepository();
        var item = new AvailabilityException
        {
            RuleExceptionId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            ExceptionDate = new DateOnly(2026, 3, 15),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        repository.Exceptions.Add(item);

        var controller = new AvailabilityExceptionsController(repository);
        var result = await controller.Delete(item.RuleExceptionId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    private sealed class FakeAvailabilityRepository : IAvailabilityRepository
    {
        public List<AvailabilityException> Exceptions { get; } = new();

        public Task<bool> HasAnyRulesAsync(CancellationToken ct) => Task.FromResult(false);
        public Task AddRuleAsync(AvailabilityRule rule, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AvailabilityRule>> GetRulesAsync(Guid tenantId, Guid serviceId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AvailabilityRule>>(Array.Empty<AvailabilityRule>());
        public Task<AvailabilityRule?> GetRuleAsync(Guid ruleId, CancellationToken ct) => Task.FromResult<AvailabilityRule?>(null);
        public Task UpdateRuleAsync(AvailabilityRule rule, CancellationToken ct) => Task.CompletedTask;
        public Task AddExceptionAsync(AvailabilityException item, CancellationToken ct)
        {
            Exceptions.Add(item);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AvailabilityException>> GetExceptionsAsync(Guid tenantId, Guid serviceId, DateOnly from, DateOnly to, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AvailabilityException>>(Exceptions);

        public Task<AvailabilityException?> GetActiveExceptionAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct)
            => Task.FromResult<AvailabilityException?>(null);

        public Task<bool> DeleteExceptionAsync(Guid ruleExceptionId, CancellationToken ct)
            => Task.FromResult(Exceptions.RemoveAll(x => x.RuleExceptionId == ruleExceptionId) > 0);

        public Task<IReadOnlyList<AvailabilityRule>> GetActiveRulesByDayAsync(Guid tenantId, Guid serviceId, int dayOfWeek, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<AvailabilityRule>>(Array.Empty<AvailabilityRule>());
    }
}

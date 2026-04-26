using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using AvailabilityService.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AvailabilityService.Tests;

public sealed class AvailabilitySlotServiceTests
{
    [Fact]
    public async Task GetSlotsAsync_WhenActiveExceptionExists_ReturnsClosedWithNoSlots()
    {
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 9);

        var repository = new FakeAvailabilityRepository
        {
            ActiveException = new AvailabilityException
            {
                RuleExceptionId = Guid.NewGuid(),
                TenantId = tenantId,
                ServiceId = serviceId,
                ExceptionDate = date,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var slotService = CreateService(repository);

        var result = await slotService.GetSlotsAsync(tenantId, serviceId, date, CancellationToken.None);

        Assert.True(result.IsClosed);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task GetSlotsAsync_WhenRulesExist_GeneratesExpectedSlots()
    {
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 9); // Monday

        var repository = new FakeAvailabilityRepository
        {
            ActiveRules = new List<AvailabilityRule>
            {
                new()
                {
                    RuleId = Guid.NewGuid(),
                    TenantId = tenantId,
                    ServiceId = serviceId,
                    DayOfWeek = 1,
                    OperatingStartTime = new TimeOnly(9, 0),
                    OperatingEndTime = new TimeOnly(10, 0),
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            }
        };

        var slotService = CreateService(repository);

        var result = await slotService.GetSlotsAsync(tenantId, serviceId, date, CancellationToken.None);

        Assert.False(result.IsClosed);
        Assert.Equal(4, result.Slots.Count);
        Assert.Equal("AVAILABLE", result.Slots[0].Status);
        Assert.Equal(3, result.Slots[0].CapacityTotal);
        Assert.Equal(3, result.Slots[0].CapacityRemaining);
        Assert.Equal(new TimeOnly(9, 0), TimeOnly.FromDateTime(result.Slots[0].StartTime.DateTime));
        Assert.Equal(new TimeOnly(10, 0), TimeOnly.FromDateTime(result.Slots[^1].EndTime.DateTime));
    }

    [Fact]
    public async Task GetSlotsAsync_WithMultipleRules_ReturnsSlotsInRuleOrder()
    {
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 9); // Monday

        var repository = new FakeAvailabilityRepository
        {
            ActiveRules = new List<AvailabilityRule>
            {
                new()
                {
                    RuleId = Guid.NewGuid(),
                    TenantId = tenantId,
                    ServiceId = serviceId,
                    DayOfWeek = 1,
                    OperatingStartTime = new TimeOnly(9, 0),
                    OperatingEndTime = new TimeOnly(9, 30),
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new()
                {
                    RuleId = Guid.NewGuid(),
                    TenantId = tenantId,
                    ServiceId = serviceId,
                    DayOfWeek = 1,
                    OperatingStartTime = new TimeOnly(13, 0),
                    OperatingEndTime = new TimeOnly(13, 30),
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            }
        };

        var slotService = CreateService(repository);

        var result = await slotService.GetSlotsAsync(tenantId, serviceId, date, CancellationToken.None);

        Assert.Equal(4, result.Slots.Count);
        Assert.Collection(result.Slots,
            slot => Assert.Equal(new TimeOnly(9, 0), TimeOnly.FromDateTime(slot.StartTime.DateTime)),
            slot => Assert.Equal(new TimeOnly(9, 15), TimeOnly.FromDateTime(slot.StartTime.DateTime)),
            slot => Assert.Equal(new TimeOnly(13, 0), TimeOnly.FromDateTime(slot.StartTime.DateTime)),
            slot => Assert.Equal(new TimeOnly(13, 15), TimeOnly.FromDateTime(slot.StartTime.DateTime)));
    }

    [Fact]
    public async Task GetSlotsAsync_WhenNoRules_ReturnsClosed()
    {
        var slotService = CreateService(new FakeAvailabilityRepository());
        var result = await slotService.GetSlotsAsync(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 9), CancellationToken.None);

        Assert.True(result.IsClosed);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task GetSlotsAsync_OnSunday_UsesDomainDaySeven()
    {
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var sunday = new DateOnly(2026, 3, 8);
        var repository = new FakeAvailabilityRepository
        {
            ActiveRules = new List<AvailabilityRule>
            {
                new()
                {
                    RuleId = Guid.NewGuid(),
                    TenantId = tenantId,
                    ServiceId = serviceId,
                    DayOfWeek = 7,
                    OperatingStartTime = new TimeOnly(9, 0),
                    OperatingEndTime = new TimeOnly(9, 30),
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            }
        };

        var slotService = CreateService(repository);
        var result = await slotService.GetSlotsAsync(tenantId, serviceId, sunday, CancellationToken.None);

        Assert.False(result.IsClosed);
        Assert.Equal(2, result.Slots.Count);
    }

    [Fact]
    public async Task GetSlotsAsync_WithInvalidTimeZone_FallsBackToUtcOffset()
    {
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 9);
        var repository = new FakeAvailabilityRepository
        {
            ActiveRules = new List<AvailabilityRule>
            {
                new()
                {
                    RuleId = Guid.NewGuid(),
                    TenantId = tenantId,
                    ServiceId = serviceId,
                    DayOfWeek = 1,
                    OperatingStartTime = new TimeOnly(9, 0),
                    OperatingEndTime = new TimeOnly(9, 15),
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            }
        };

        var options = Options.Create(new AvailabilityOptions
        {
            SlotDurationMinutes = 15,
            CapacityPerSlot = 1,
            TimeZone = "Invalid/Timezone"
        });
        var slotService = new AvailabilitySlotService(repository, options);

        var result = await slotService.GetSlotsAsync(tenantId, serviceId, date, CancellationToken.None);

        Assert.Single(result.Slots);
        Assert.Equal(TimeSpan.Zero, result.Slots[0].StartTime.Offset);
    }

    private static AvailabilitySlotService CreateService(IAvailabilityRepository repository)
    {
        var options = Options.Create(new AvailabilityOptions
        {
            SlotDurationMinutes = 15,
            CapacityPerSlot = 3,
            TimeZone = "UTC"
        });

        return new AvailabilitySlotService(repository, options);
    }

    private sealed class FakeAvailabilityRepository : IAvailabilityRepository
    {
        public AvailabilityException? ActiveException { get; set; }
        public IReadOnlyList<AvailabilityRule> ActiveRules { get; set; } = Array.Empty<AvailabilityRule>();

        public Task<bool> HasAnyRulesAsync(CancellationToken ct) => Task.FromResult(false);
        public Task AddRuleAsync(AvailabilityRule rule, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AvailabilityRule>> GetRulesAsync(Guid tenantId, Guid serviceId, CancellationToken ct) => Task.FromResult<IReadOnlyList<AvailabilityRule>>(Array.Empty<AvailabilityRule>());
        public Task<AvailabilityRule?> GetRuleAsync(Guid ruleId, CancellationToken ct) => Task.FromResult<AvailabilityRule?>(null);
        public Task UpdateRuleAsync(AvailabilityRule rule, CancellationToken ct) => Task.CompletedTask;
        public Task AddExceptionAsync(AvailabilityException item, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AvailabilityException>> GetExceptionsAsync(Guid tenantId, Guid serviceId, DateOnly from, DateOnly to, CancellationToken ct) => Task.FromResult<IReadOnlyList<AvailabilityException>>(Array.Empty<AvailabilityException>());
        public Task<AvailabilityException?> GetActiveExceptionAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct) => Task.FromResult(ActiveException);
        public Task<bool> DeleteExceptionAsync(Guid ruleExceptionId, CancellationToken ct) => Task.FromResult(false);
        public Task<IReadOnlyList<AvailabilityRule>> GetActiveRulesByDayAsync(Guid tenantId, Guid serviceId, int dayOfWeek, CancellationToken ct) => Task.FromResult(ActiveRules);
    }
}

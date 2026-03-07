using AvailabilityService.Controllers;
using AvailabilityService.Models;
using AvailabilityService.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AvailabilityService.Tests;

public sealed class AvailabilitySlotsControllerTests
{
    [Fact]
    public async Task Get_WithInvalidDate_ReturnsBadRequest()
    {
        var controller = new AvailabilitySlotsController(new FakeAvailabilitySlotService());

        var result = await controller.Get(Guid.NewGuid(), Guid.NewGuid(), "invalid", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_WithValidDate_ReturnsOk()
    {
        var controller = new AvailabilitySlotsController(new FakeAvailabilitySlotService());
        var tenantId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();

        var result = await controller.Get(tenantId, serviceId, "2026-03-09", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<SlotsResponse>(ok.Value);
        Assert.Equal(tenantId, payload.TenantId);
        Assert.Equal(serviceId, payload.ServiceId);
    }

    private sealed class FakeAvailabilitySlotService : IAvailabilitySlotService
    {
        public Task<SlotsResponse> GetSlotsAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct)
            => Task.FromResult(new SlotsResponse
            {
                TenantId = tenantId,
                ServiceId = serviceId,
                Date = date,
                IsClosed = true,
                Slots = new List<SlotDto>()
            });
    }
}

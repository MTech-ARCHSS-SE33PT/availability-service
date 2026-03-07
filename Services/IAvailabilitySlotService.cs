using AvailabilityService.Models;

namespace AvailabilityService.Services;

public interface IAvailabilitySlotService
{
    Task<SlotsResponse> GetSlotsAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct);
}

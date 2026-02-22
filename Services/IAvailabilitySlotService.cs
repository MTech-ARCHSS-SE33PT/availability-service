using AvailabilityService.Models;

namespace AvailabilityService.Services;

public interface IAvailabilitySlotService
{
    SlotsResponse GetSlots(Guid tenantId, Guid serviceId, DateOnly date);
}


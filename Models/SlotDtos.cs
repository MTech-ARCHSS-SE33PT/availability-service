namespace AvailabilityService.Models;

public class SlotDto
{
    public string SlotId { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public int CapacityTotal { get; set; }
    public int CapacityRemaining { get; set; }
    public string Status { get; set; } = "AVAILABLE";
}

public class SlotsResponse
{
    public DateOnly Date { get; set; }
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public bool IsClosed { get; set; }
    public List<SlotDto> Slots { get; set; } = new();
}


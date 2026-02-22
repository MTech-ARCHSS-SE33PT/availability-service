namespace AvailabilityService.Models;

public class AvailabilityOptions
{
    public int SlotDurationMinutes { get; set; } = 15;
    public int CapacityPerSlot { get; set; } = 3;
    public string TimeZone { get; set; } = "Asia/Singapore";
}


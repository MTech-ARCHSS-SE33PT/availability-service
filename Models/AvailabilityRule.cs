namespace AvailabilityService.Models;

public class AvailabilityRule
{
    public Guid RuleId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeOnly OperatingStartTime { get; set; }
    public TimeOnly OperatingEndTime { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}


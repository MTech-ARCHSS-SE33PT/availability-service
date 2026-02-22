namespace AvailabilityService.Models;

public class CreateRuleRequest
{
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public int DayOfWeek { get; set; }
    public string OperatingStartTime { get; set; } = string.Empty;
    public string OperatingEndTime { get; set; } = string.Empty;
}

public class UpdateRuleRequest
{
    public string? OperatingStartTime { get; set; }
    public string? OperatingEndTime { get; set; }
    public bool? IsActive { get; set; }
}


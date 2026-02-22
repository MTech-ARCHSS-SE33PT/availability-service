namespace AvailabilityService.Models;

public class AvailabilityException
{
    public Guid RuleExceptionId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public DateOnly ExceptionDate { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}


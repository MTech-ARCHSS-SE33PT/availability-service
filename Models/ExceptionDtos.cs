namespace AvailabilityService.Models;

public class CreateExceptionRequest
{
    public Guid TenantId { get; set; }
    public Guid ServiceId { get; set; }
    public string ExceptionDate { get; set; } = string.Empty;
    public string? Reason { get; set; }
}


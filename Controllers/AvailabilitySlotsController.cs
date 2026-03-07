using AvailabilityService.Models;
using AvailabilityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AvailabilityService.Controllers;

[ApiController]
[Route("availability/slots")]
public class AvailabilitySlotsController : ControllerBase
{
    private readonly IAvailabilitySlotService _slotService;

    public AvailabilitySlotsController(IAvailabilitySlotService slotService)
    {
        _slotService = slotService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid tenantId, [FromQuery] Guid serviceId, [FromQuery] string date, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { error = "date must be valid date" });
        }

        var response = await _slotService.GetSlotsAsync(tenantId, serviceId, parsedDate, ct);
        return Ok(response);
    }
}

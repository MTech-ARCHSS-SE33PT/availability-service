using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.AspNetCore.Mvc;

namespace AvailabilityService.Controllers;

[ApiController]
[Route("availability/exceptions")]
public class AvailabilityExceptionsController : ControllerBase
{
    private readonly IAvailabilityRepository _repository;

    public AvailabilityExceptionsController(IAvailabilityRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExceptionRequest request, CancellationToken ct)
    {
        if (!DateOnly.TryParse(request.ExceptionDate, out var exceptionDate))
        {
            return BadRequest(Error("exception_date must be valid date"));
        }

        if (request.Reason is { Length: > 100 })
        {
            return BadRequest(Error("reason must be 100 characters or fewer"));
        }

        var item = new AvailabilityException
        {
            RuleExceptionId = Guid.NewGuid(),
            TenantId = request.TenantId,
            ServiceId = request.ServiceId,
            ExceptionDate = exceptionDate,
            Reason = request.Reason,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddExceptionAsync(item, ct);
        return Created($"/availability/exceptions/{item.RuleExceptionId}", item);
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid tenantId,
        [FromQuery] Guid serviceId,
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken ct)
    {
        if (!DateOnly.TryParse(from, out var fromDate))
        {
            return BadRequest(Error("from must be valid date"));
        }

        if (!DateOnly.TryParse(to, out var toDate))
        {
            return BadRequest(Error("to must be valid date"));
        }

        if (toDate < fromDate)
        {
            return BadRequest(Error("to must be >= from"));
        }

        var results = await _repository.GetExceptionsAsync(tenantId, serviceId, fromDate, toDate, ct);

        return Ok(results);
    }

    [HttpDelete("{ruleExceptionId:guid}")]
    public async Task<IActionResult> Delete(Guid ruleExceptionId, CancellationToken ct)
    {
        var deleted = await _repository.DeleteExceptionAsync(ruleExceptionId, ct);
        if (!deleted)
        {
            return NotFound(Error("exceptionId not found"));
        }

        return NoContent();
    }

    private static object Error(string message) => new { error = message };
}

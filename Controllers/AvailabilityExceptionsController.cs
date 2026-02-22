using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.AspNetCore.Mvc;

namespace AvailabilityService.Controllers;

[ApiController]
[Route("availability/exceptions")]
public class AvailabilityExceptionsController : ControllerBase
{
    private readonly InMemoryAvailabilityStore _store;

    public AvailabilityExceptionsController(InMemoryAvailabilityStore store)
    {
        _store = store;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateExceptionRequest request)
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

        _store.Exceptions[item.RuleExceptionId] = item;
        return Created($"/availability/exceptions/{item.RuleExceptionId}", item);
    }

    [HttpGet]
    public IActionResult Get(
        [FromQuery] Guid tenantId,
        [FromQuery] Guid serviceId,
        [FromQuery] string from,
        [FromQuery] string to)
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

        var results = _store.Exceptions.Values
            .Where(e => e.TenantId == tenantId &&
                        e.ServiceId == serviceId &&
                        e.ExceptionDate >= fromDate &&
                        e.ExceptionDate <= toDate)
            .OrderBy(e => e.ExceptionDate)
            .ToList();

        return Ok(results);
    }

    [HttpDelete("{ruleExceptionId:guid}")]
    public IActionResult Delete(Guid ruleExceptionId)
    {
        if (!_store.Exceptions.TryRemove(ruleExceptionId, out _))
        {
            return NotFound(Error("exceptionId not found"));
        }

        return NoContent();
    }

    private static object Error(string message) => new { error = message };
}


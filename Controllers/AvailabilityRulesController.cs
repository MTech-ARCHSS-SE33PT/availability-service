using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.AspNetCore.Mvc;

namespace AvailabilityService.Controllers;

[ApiController]
[Route("availability/rules")]
public class AvailabilityRulesController : ControllerBase
{
    private readonly InMemoryAvailabilityStore _store;

    public AvailabilityRulesController(InMemoryAvailabilityStore store)
    {
        _store = store;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateRuleRequest request)
    {
        if (request.DayOfWeek < 1 || request.DayOfWeek > 7)
        {
            return BadRequest(Error("day_of_week must be 1..7"));
        }

        if (!TimeOnly.TryParse(request.OperatingStartTime, out var start))
        {
            return BadRequest(Error("operating_start_time must be valid HH:mm"));
        }

        if (!TimeOnly.TryParse(request.OperatingEndTime, out var end))
        {
            return BadRequest(Error("operating_end_time must be valid HH:mm"));
        }

        if (end <= start)
        {
            return BadRequest(Error("operating_end_time must be > operating_start_time"));
        }

        var rule = new AvailabilityRule
        {
            RuleId = Guid.NewGuid(),
            TenantId = request.TenantId,
            ServiceId = request.ServiceId,
            DayOfWeek = request.DayOfWeek,
            OperatingStartTime = start,
            OperatingEndTime = end,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _store.Rules[rule.RuleId] = rule;
        return Created($"/availability/rules/{rule.RuleId}", rule);
    }

    [HttpGet]
    public IActionResult Get([FromQuery] Guid tenantId, [FromQuery] Guid serviceId)
    {
        var rules = _store.Rules.Values
            .Where(r => r.TenantId == tenantId && r.ServiceId == serviceId)
            .OrderBy(r => r.DayOfWeek)
            .ThenBy(r => r.OperatingStartTime)
            .ToList();

        return Ok(rules);
    }

    [HttpPatch("{ruleId:guid}")]
    public IActionResult Patch(Guid ruleId, [FromBody] UpdateRuleRequest request)
    {
        if (!_store.Rules.TryGetValue(ruleId, out var rule))
        {
            return NotFound(Error("ruleId not found"));
        }

        var start = rule.OperatingStartTime;
        var end = rule.OperatingEndTime;

        if (request.OperatingStartTime is not null)
        {
            if (!TimeOnly.TryParse(request.OperatingStartTime, out start))
            {
                return BadRequest(Error("operating_start_time must be valid HH:mm"));
            }
        }

        if (request.OperatingEndTime is not null)
        {
            if (!TimeOnly.TryParse(request.OperatingEndTime, out end))
            {
                return BadRequest(Error("operating_end_time must be valid HH:mm"));
            }
        }

        if (end <= start)
        {
            return BadRequest(Error("operating_end_time must be > operating_start_time"));
        }

        rule.OperatingStartTime = start;
        rule.OperatingEndTime = end;
        if (request.IsActive.HasValue)
        {
            rule.IsActive = request.IsActive.Value;
        }

        return Ok(rule);
    }

    [HttpPost("{ruleId:guid}/deactivate")]
    public IActionResult Deactivate(Guid ruleId)
    {
        if (!_store.Rules.TryGetValue(ruleId, out var rule))
        {
            return NotFound(Error("ruleId not found"));
        }

        rule.IsActive = false;
        return Ok(rule);
    }

    private static object Error(string message) => new { error = message };
}


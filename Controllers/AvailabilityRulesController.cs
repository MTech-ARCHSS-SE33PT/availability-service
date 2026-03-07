using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.AspNetCore.Mvc;

namespace AvailabilityService.Controllers;

[ApiController]
[Route("availability/rules")]
public class AvailabilityRulesController : ControllerBase
{
    private readonly IAvailabilityRepository _repository;

    public AvailabilityRulesController(IAvailabilityRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRuleRequest request, CancellationToken ct)
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

        await _repository.AddRuleAsync(rule, ct);
        return Created($"/availability/rules/{rule.RuleId}", rule);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid tenantId, [FromQuery] Guid serviceId, CancellationToken ct)
    {
        var rules = await _repository.GetRulesAsync(tenantId, serviceId, ct);

        return Ok(rules);
    }

    [HttpPatch("{ruleId:guid}")]
    public async Task<IActionResult> Patch(Guid ruleId, [FromBody] UpdateRuleRequest request, CancellationToken ct)
    {
        var rule = await _repository.GetRuleAsync(ruleId, ct);
        if (rule is null)
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

        await _repository.UpdateRuleAsync(rule, ct);
        return Ok(rule);
    }

    [HttpPost("{ruleId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid ruleId, CancellationToken ct)
    {
        var rule = await _repository.GetRuleAsync(ruleId, ct);
        if (rule is null)
        {
            return NotFound(Error("ruleId not found"));
        }

        rule.IsActive = false;
        await _repository.UpdateRuleAsync(rule, ct);
        return Ok(rule);
    }

    private static object Error(string message) => new { error = message };
}

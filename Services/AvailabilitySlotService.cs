using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.Extensions.Options;

namespace AvailabilityService.Services;

public class AvailabilitySlotService : IAvailabilitySlotService
{
    private readonly IAvailabilityRepository _repository;
    private readonly AvailabilityOptions _options;
    private readonly TimeZoneInfo _timeZone;

    public AvailabilitySlotService(
        IAvailabilityRepository repository,
        IOptions<AvailabilityOptions> options)
    {
        _repository = repository;
        _options = options.Value;
        _timeZone = ResolveTimeZone(_options.TimeZone);
    }

    public async Task<SlotsResponse> GetSlotsAsync(Guid tenantId, Guid serviceId, DateOnly date, CancellationToken ct)
    {
        var activeException = await _repository.GetActiveExceptionAsync(tenantId, serviceId, date, ct);

        if (activeException is not null)
        {
            return new SlotsResponse
            {
                Date = date,
                TenantId = tenantId,
                ServiceId = serviceId,
                IsClosed = true,
                Slots = new List<SlotDto>()
            };
        }

        var dayOfWeek = ToDomainDayOfWeek(date.DayOfWeek);
        var rules = await _repository.GetActiveRulesByDayAsync(tenantId, serviceId, dayOfWeek, ct);

        var slots = new List<SlotDto>();
        var slotDuration = TimeSpan.FromMinutes(_options.SlotDurationMinutes);

        foreach (var rule in rules)
        {
            var current = rule.OperatingStartTime;
            while (current.AddMinutes(_options.SlotDurationMinutes) <= rule.OperatingEndTime)
            {
                var next = current.AddMinutes(_options.SlotDurationMinutes);
                slots.Add(new SlotDto
                {
                    SlotId = $"{tenantId}|{serviceId}|{date:yyyyMMdd}|{current:HHmm}",
                    StartTime = ToDateTimeOffset(date, current),
                    EndTime = ToDateTimeOffset(date, next),
                    CapacityTotal = _options.CapacityPerSlot,
                    CapacityRemaining = _options.CapacityPerSlot,
                    Status = "AVAILABLE"
                });

                current = current.Add(slotDuration);
            }
        }

        return new SlotsResponse
        {
            Date = date,
            TenantId = tenantId,
            ServiceId = serviceId,
            IsClosed = slots.Count == 0,
            Slots = slots
        };
    }

    private DateTimeOffset ToDateTimeOffset(DateOnly date, TimeOnly time)
    {
        var localDateTime = date.ToDateTime(time);
        var offset = _timeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset);
    }

    private static int ToDomainDayOfWeek(DayOfWeek dayOfWeek)
    {
        return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}

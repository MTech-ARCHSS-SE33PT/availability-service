using AvailabilityService.Infrastructure;
using AvailabilityService.Models;
using Microsoft.Extensions.Options;

namespace AvailabilityService.Services;

public class AvailabilitySlotService : IAvailabilitySlotService
{
    private readonly InMemoryAvailabilityStore _store;
    private readonly AvailabilityOptions _options;
    private readonly TimeZoneInfo _timeZone;

    public AvailabilitySlotService(
        InMemoryAvailabilityStore store,
        IOptions<AvailabilityOptions> options)
    {
        _store = store;
        _options = options.Value;
        _timeZone = ResolveTimeZone(_options.TimeZone);
    }

    public SlotsResponse GetSlots(Guid tenantId, Guid serviceId, DateOnly date)
    {
        var activeException = _store.Exceptions.Values.FirstOrDefault(e =>
            e.IsActive &&
            e.TenantId == tenantId &&
            e.ServiceId == serviceId &&
            e.ExceptionDate == date);

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
        var rules = _store.Rules.Values
            .Where(r => r.IsActive &&
                        r.TenantId == tenantId &&
                        r.ServiceId == serviceId &&
                        r.DayOfWeek == dayOfWeek)
            .OrderBy(r => r.OperatingStartTime)
            .ThenBy(r => r.OperatingEndTime)
            .ToList();

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


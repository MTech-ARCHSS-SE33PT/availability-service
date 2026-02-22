using System.Collections.Concurrent;
using AvailabilityService.Models;

namespace AvailabilityService.Infrastructure;

public class InMemoryAvailabilityStore
{
    public ConcurrentDictionary<Guid, AvailabilityRule> Rules { get; } = new();
    public ConcurrentDictionary<Guid, AvailabilityException> Exceptions { get; } = new();
}


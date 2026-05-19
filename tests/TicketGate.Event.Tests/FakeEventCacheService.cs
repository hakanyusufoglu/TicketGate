using TicketGate.Core.Domain;
using TicketGate.Event.Features.Events.Queries.GetEventById;
using TicketGate.Event.Infrastructure.Cache;

namespace TicketGate.Event.Tests;

internal sealed class FakeEventCacheService(EventDetailDto? cachedEvent = null) : IEventCacheService
{
    public int GetEventCalls { get; private set; }

    public int SetEventCalls { get; private set; }

    public int GetSeatMapCalls { get; private set; }

    public int SetSeatMapCalls { get; private set; }

    public int InvalidateEventCalls { get; private set; }

    public Guid? StoredEventId { get; private set; }

    public EventDetailDto? StoredEvent { get; private set; }

    public Guid? InvalidatedEventId { get; private set; }

    public Task<EventDetailDto?> GetEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        GetEventCalls++;
        return Task.FromResult(cachedEvent);
    }

    public Task SetEventAsync(Guid eventId, EventDetailDto dto, CancellationToken cancellationToken)
    {
        SetEventCalls++;
        StoredEventId = eventId;
        StoredEvent = dto;
        return Task.CompletedTask;
    }

    public Task<SeatMap?> GetSeatMapAsync(Guid venueId, CancellationToken cancellationToken)
    {
        GetSeatMapCalls++;
        return Task.FromResult<SeatMap?>(null);
    }

    public Task SetSeatMapAsync(Guid venueId, SeatMap seatMap, CancellationToken cancellationToken)
    {
        SetSeatMapCalls++;
        return Task.CompletedTask;
    }

    public Task InvalidateEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        InvalidateEventCalls++;
        InvalidatedEventId = eventId;
        return Task.CompletedTask;
    }
}


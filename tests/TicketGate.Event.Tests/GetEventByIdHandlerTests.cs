using FluentAssertions;
using TicketGate.Core.Domain;
using TicketGate.Event.Features.Events.Queries.GetEventById;
using TicketGate.Event.Infrastructure.Cache;
using EventEntity = TicketGate.Event.Domain.Entities.Event;

namespace TicketGate.Event.Tests;

public sealed class GetEventByIdHandlerTests
{
    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedEventWithoutDatabaseRead()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var cached = CreateEventDetailDto(Guid.NewGuid(), "Cached Event");
        var cache = new FakeEventCacheService(cached);
        var handler = new GetEventByIdHandler(db, cache);

        var result = await handler.Handle(new GetEventByIdQuery(cached.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cached);
        cache.GetEventCalls.Should().Be(1);
        cache.SetEventCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CacheMiss_ReadsDatabaseAndStoresEventInCache()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var venue = await EventTestFactory.SeedVenueAsync(db);
        var performer = await EventTestFactory.SeedPerformerAsync(db);
        var eventEntity = EventEntity.Create(
            "Database Event",
            "Description",
            venue.Id,
            performer.Id,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2));

        await db.Events.AddAsync(eventEntity, CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var cache = new FakeEventCacheService();
        var handler = new GetEventByIdHandler(db, cache);

        var result = await handler.Handle(new GetEventByIdQuery(eventEntity.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Database Event");
        cache.GetEventCalls.Should().Be(1);
        cache.SetEventCalls.Should().Be(1);
        cache.StoredEventId.Should().Be(eventEntity.Id);
        cache.StoredEvent!.Name.Should().Be("Database Event");
    }

    private static EventDetailDto CreateEventDetailDto(Guid id, string name)
    {
        return new EventDetailDto(
            id,
            name,
            "Description",
            "Venue",
            "Istanbul",
            new SeatMap(),
            "Performer",
            "Bio",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            true);
    }
}


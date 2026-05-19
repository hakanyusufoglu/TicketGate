using FluentAssertions;
using TicketGate.Event.Features.Events.Commands.UpdateEvent;
using EventEntity = TicketGate.Event.Domain.Entities.Event;

namespace TicketGate.Event.Tests;

public sealed class UpdateEventHandlerTests
{
    [Fact]
    public async Task Handle_DraftEvent_UpdatesEventAndInvalidatesCache()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var venue = await EventTestFactory.SeedVenueAsync(db);
        var performer = await EventTestFactory.SeedPerformerAsync(db);
        var eventEntity = EventEntity.Create(
            "Old Name",
            "Old Description",
            venue.Id,
            performer.Id,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2));

        await db.Events.AddAsync(eventEntity, CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var cache = new FakeEventCacheService();
        var handler = new UpdateEventHandler(db, cache);
        var startsAt = DateTime.UtcNow.AddDays(8);

        var result = await handler.Handle(
            new UpdateEventCommand(eventEntity.Id, "New Name", "New Description", startsAt, startsAt.AddHours(3)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cache.InvalidateEventCalls.Should().Be(1);
        cache.InvalidatedEventId.Should().Be(eventEntity.Id);
    }
}


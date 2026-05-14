using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Event.Features.Events.Commands.PublishEvent;
using EventEntity = TicketGate.Event.Domain.Entities.Event;

namespace TicketGate.Event.Tests;

public sealed class PublishEventHandlerTests
{
    [Fact]
    public async Task Handle_UnpublishedEvent_SetsIsPublished()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var venue = await EventTestFactory.SeedVenueAsync(db);
        var performer = await EventTestFactory.SeedPerformerAsync(db);
        var eventEntity = EventEntity.Create(
            "Jazz Night",
            "Live jazz performance",
            venue.Id,
            performer.Id,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2));

        await db.Events.AddAsync(eventEntity, CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new PublishEventHandler(db);

        var result = await handler.Handle(new PublishEventCommand(eventEntity.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var publishedEvent = await db.Events.SingleAsync(CancellationToken.None);
        publishedEvent.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AlreadyPublished_Returns409()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var venue = await EventTestFactory.SeedVenueAsync(db);
        var performer = await EventTestFactory.SeedPerformerAsync(db);
        var eventEntity = EventEntity.Create(
            "Jazz Night",
            "Live jazz performance",
            venue.Id,
            performer.Id,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2));
        eventEntity.Publish();

        await db.Events.AddAsync(eventEntity, CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new PublishEventHandler(db);

        var result = await handler.Handle(new PublishEventCommand(eventEntity.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Conflict);
        result.Error.Code.Should().Be("Event.AlreadyPublished");
    }
}

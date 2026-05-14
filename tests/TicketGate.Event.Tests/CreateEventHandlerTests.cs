using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Event.Features.Events.Commands.CreateEvent;

namespace TicketGate.Event.Tests;

public sealed class CreateEventHandlerTests
{
    [Fact]
    public async Task Handle_ValidEvent_ReturnsEventId()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var venue = await EventTestFactory.SeedVenueAsync(db);
        var performer = await EventTestFactory.SeedPerformerAsync(db);
        var handler = new CreateEventHandler(db);
        var command = new CreateEventCommand(
            "Jazz Night",
            "Live jazz performance",
            venue.Id,
            performer.Id,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var createdEvent = await db.Events.SingleAsync(CancellationToken.None);
        createdEvent.Id.Should().Be(result.Value);
        createdEvent.VenueId.Should().Be(venue.Id);
        createdEvent.PerformerId.Should().Be(performer.Id);
        createdEvent.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_VenueNotFound_Returns404()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var performer = await EventTestFactory.SeedPerformerAsync(db);
        var missingVenueId = Guid.NewGuid();
        var handler = new CreateEventHandler(db);
        var command = new CreateEventCommand(
            "Jazz Night",
            "Live jazz performance",
            missingVenueId,
            performer.Id,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("venue.not_found");
        (await db.Events.CountAsync(CancellationToken.None)).Should().Be(0);
    }
}

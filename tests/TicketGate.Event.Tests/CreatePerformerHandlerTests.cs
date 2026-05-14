using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketGate.Event.Features.Performers.Commands.CreatePerformer;

namespace TicketGate.Event.Tests;

public sealed class CreatePerformerHandlerTests
{
    [Fact]
    public async Task Handle_ValidPerformer_ReturnsPerformerId()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var handler = new CreatePerformerHandler(db);
        var command = new CreatePerformerCommand("Main Artist", "Artist bio");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var performer = await db.Performers.SingleAsync(CancellationToken.None);
        performer.Id.Should().Be(result.Value);
        performer.Name.Should().Be(command.Name);
    }
}

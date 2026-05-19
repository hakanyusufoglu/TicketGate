using FluentAssertions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Features.Tickets.Commands.GenerateTickets;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Domain;

namespace TicketGate.Booking.Tests.Features;

/// <summary>
/// GenerateTickets slice'inin seat map hiyerarsisine gore bilet uretimini dogrular.
/// Gercek PostgreSQL kullanilir; bulk insert ve conflict kontrolu EF konfigurasyonuyla birlikte test edilir.
/// </summary>
public sealed class GenerateTicketsHandlerTests : BookingIntegrationTestBase
{
    [Fact]
    public async Task Handle_ShouldCreateTicketForEachSeatInSeatMap()
    {
        var eventId = Guid.NewGuid();
        var sender = Services.GetRequiredService<IMediator>();

        var result = await sender.Send(new GenerateTicketsCommand(eventId, CreateSeatMap()));

        result.IsSuccess.Should().BeTrue();
        result.Value!.TicketCount.Should().Be(5);

        var db = Services.GetRequiredService<BookingDbContext>();
        var tickets = await db.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.EventId == eventId)
            .OrderBy(ticket => ticket.Seat)
            .ToListAsync();

        tickets.Should().HaveCount(5);
        tickets.Should().OnlyContain(ticket => ticket.Status == TicketStatus.Available);
        tickets.Select(ticket => ticket.Seat)
            .Should().BeEquivalentTo(["NORMAL-B-1", "NORMAL-B-2", "VIP-A-1", "VIP-A-2", "VIP-A-3"]);
        tickets.Single(ticket => ticket.Seat == "VIP-A-1").Price.Should().Be(500m);
        tickets.Single(ticket => ticket.Seat == "NORMAL-B-1").Price.Should().Be(300m);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenTicketsAlreadyGeneratedForEvent()
    {
        var eventId = Guid.NewGuid();
        var sender = Services.GetRequiredService<IMediator>();

        await sender.Send(new GenerateTicketsCommand(eventId, CreateSeatMap()));

        var result = await sender.Send(new GenerateTicketsCommand(eventId, CreateSeatMap()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Tickets.AlreadyGenerated");
    }

    private static SeatMap CreateSeatMap()
    {
        return new SeatMap
        {
            Sections =
            [
                new Section(
                    Id: "VIP",
                    Name: "VIP Alan",
                    Rows: [new Row("A", [1, 2, 3])],
                    Price: 500m),
                new Section(
                    Id: "NORMAL",
                    Name: "Normal Alan",
                    Rows: [new Row("B", [1, 2])],
                    Price: 300m)
            ]
        };
    }
}

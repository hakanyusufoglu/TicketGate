using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Domain;
using TicketGate.Event.Domain.Entities;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Tests;

internal static class EventTestFactory
{
    public static EventDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new EventDbContext(options);
    }

    public static async Task<Venue> SeedVenueAsync(
        EventDbContext db,
        string name = "Main Hall",
        string location = "Istanbul",
        CancellationToken cancellationToken = default)
    {
        var venue = Venue.Create(name, location, CreateSeatMap());

        await db.Venues.AddAsync(venue, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return venue;
    }

    public static async Task<Performer> SeedPerformerAsync(
        EventDbContext db,
        string name = "Performer",
        CancellationToken cancellationToken = default)
    {
        var performer = Performer.Create(name, "Performer bio");

        await db.Performers.AddAsync(performer, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return performer;
    }

    private static SeatMap CreateSeatMap()
    {
        return new SeatMap
        {
            Sections =
            [
                new Section(
                    Id: "TEST",
                    Name: "Test Section",
                    Rows: [new Row("A", [1])],
                    Price: 100m)
            ]
        };
    }
}

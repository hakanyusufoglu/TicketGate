using FluentAssertions;
using TicketGate.Event.Features.Events.Queries.GetEventList;
using EventEntity = TicketGate.Event.Domain.Entities.Event;

namespace TicketGate.Event.Tests;

public sealed class GetEventListHandlerTests
{
    [Fact]
    public async Task Handle_WithSearch_FiltersCorrectly()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var handler = new GetEventListHandler(db);
        await SeedPublishedEventAsync(db, "Ankara Rock", "Ankara");
        await SeedPublishedEventAsync(db, "Istanbul Jazz", "Istanbul");

        var result = await handler.Handle(
            new GetEventListQuery(1, 10, "jazz", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].Name.Should().Be("Istanbul Jazz");
    }

    [Fact]
    public async Task Handle_WithCity_FiltersCorrectly()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var handler = new GetEventListHandler(db);
        await SeedPublishedEventAsync(db, "Ankara Rock", "Ankara");
        await SeedPublishedEventAsync(db, "Istanbul Jazz", "Istanbul");

        var result = await handler.Handle(
            new GetEventListQuery(1, 10, null, "istanbul", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].VenueLocation.Should().Be("Istanbul");
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var handler = new GetEventListHandler(db);
        await SeedPublishedEventAsync(db, "Event 1", "Istanbul", DateTime.UtcNow.AddDays(1));
        await SeedPublishedEventAsync(db, "Event 2", "Istanbul", DateTime.UtcNow.AddDays(2));
        await SeedPublishedEventAsync(db, "Event 3", "Istanbul", DateTime.UtcNow.AddDays(3));

        var result = await handler.Handle(
            new GetEventListQuery(2, 1, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(3);
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Name.Should().Be("Event 2");
    }

    [Fact]
    public async Task Handle_OnlyReturnsPublishedEvents()
    {
        await using var db = EventTestFactory.CreateDbContext();
        var handler = new GetEventListHandler(db);
        await SeedPublishedEventAsync(db, "Published Event", "Istanbul");
        await SeedEventAsync(db, "Draft Event", "Istanbul", publish: false);

        var result = await handler.Handle(
            new GetEventListQuery(1, 10, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        result.Value.Items[0].Name.Should().Be("Published Event");
    }

    private static Task SeedPublishedEventAsync(
        Infrastructure.Persistence.EventDbContext db,
        string name,
        string location,
        DateTime? startsAt = null)
    {
        return SeedEventAsync(db, name, location, publish: true, startsAt);
    }

    private static async Task SeedEventAsync(
        Infrastructure.Persistence.EventDbContext db,
        string name,
        string location,
        bool publish,
        DateTime? startsAt = null)
    {
        var venue = await EventTestFactory.SeedVenueAsync(db, name: $"{name} Venue", location: location);
        var performer = await EventTestFactory.SeedPerformerAsync(db, name: $"{name} Performer");
        var actualStartsAt = startsAt ?? DateTime.UtcNow.AddDays(7);
        var eventEntity = EventEntity.Create(
            name,
            "Description",
            venue.Id,
            performer.Id,
            actualStartsAt,
            actualStartsAt.AddHours(2));

        if (publish)
        {
            eventEntity.Publish();
        }

        await db.Events.AddAsync(eventEntity, CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);
    }
}

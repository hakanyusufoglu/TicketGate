using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Domain;
using TicketGate.Event.Domain.Entities;
using TicketGate.Event.Infrastructure.Persistence;
using EventEntity = TicketGate.Event.Domain.Entities.Event;

namespace TicketGate.API.Seed;

/// <summary>
/// Development ortami seed data servisi. Uygulama baslarken test icin gerekli venue, performer ve event verilerini olusturur.
/// Veri zaten varsa tekrar eklenmez; idempotent calisir.
/// Ticket seed'i yoktur; ticket'lar generate endpoint'i ile olusturulur.
/// </summary>
public static class SeedDataService
{
    private const string EntityIdPropertyName = "Id";

    /// <summary>
    /// Seed verilerini sirasiyla ekler.
    /// Sira onemlidir: Venue, Performer, Event.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();

        await SeedVenueAsync(db);
        await SeedPerformerAsync(db);
        await SeedEventAsync(db);
    }

    /// <summary>
    /// Volkswagen Arena venue kaydini yeni SeatMap hiyerarsisiyle olusturur.
    /// Kayit zaten varsa tekrar eklemeden atlar.
    /// </summary>
    private static async Task SeedVenueAsync(EventDbContext db)
    {
        if (await db.Venues.AnyAsync(venue => venue.Id == SeedGuids.VenueId))
        {
            return;
        }

        var seatMap = new SeatMap
        {
            Sections =
            [
                new Section(
                    Id: "VIP",
                    Name: "VIP Alan",
                    Rows:
                    [
                        new Row("A", [1, 2, 3, 4, 5]),
                        new Row("B", [1, 2, 3, 4, 5])
                    ],
                    Price: 500m),
                new Section(
                    Id: "NORMAL",
                    Name: "Normal Alan",
                    Rows:
                    [
                        new Row("C", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]),
                        new Row("D", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
                    ],
                    Price: 300m),
                new Section(
                    Id: "EKONOMI",
                    Name: "Ekonomi Alan",
                    Rows:
                    [
                        new Row("E", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]),
                        new Row("F", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
                    ],
                    Price: 150m)
            ]
        };

        var venue = Venue.Create(
            "Volkswagen Arena",
            "Istanbul, Turkiye",
            seatMap);

        db.Venues.Add(venue);
        SetEntityId(db, venue, SeedGuids.VenueId);
        await db.SaveChangesAsync();
        Console.WriteLine("[Seed] Venue olusturuldu: Volkswagen Arena");
    }

    /// <summary>
    /// Tarkan performer kaydini olusturur.
    /// Kayit zaten varsa tekrar eklemeden atlar.
    /// </summary>
    private static async Task SeedPerformerAsync(EventDbContext db)
    {
        if (await db.Performers.AnyAsync(performer => performer.Id == SeedGuids.PerformerId))
        {
            return;
        }

        var performer = Performer.Create(
            "Tarkan",
            "Turk pop muziginin efsanevi ismi.");

        db.Performers.Add(performer);
        SetEntityId(db, performer, SeedGuids.PerformerId);
        await db.SaveChangesAsync();
        Console.WriteLine("[Seed] Performer olusturuldu: Tarkan");
    }

    /// <summary>
    /// Tarkan Konseri 2026 event kaydini olusturur ve yayinlar.
    /// Kayit zaten varsa tekrar eklemeden atlar.
    /// </summary>
    private static async Task SeedEventAsync(EventDbContext db)
    {
        if (await db.Events.AnyAsync(eventEntity => eventEntity.Id == SeedGuids.EventId))
        {
            return;
        }

        var startsAt = DateTime.UtcNow.AddMonths(3);
        var eventEntity = EventEntity.Create(
            "Tarkan Konseri 2026",
            "Unutulmaz bir gece sizi bekliyor.",
            SeedGuids.VenueId,
            SeedGuids.PerformerId,
            startsAt,
            startsAt.AddHours(3));

        eventEntity.Publish();
        db.Events.Add(eventEntity);
        SetEntityId(db, eventEntity, SeedGuids.EventId);
        await db.SaveChangesAsync();
        Console.WriteLine("[Seed] Event olusturuldu: Tarkan Konseri 2026 (published)");
    }

    /// <summary>
    /// Seed entity kimligini EF Core change tracker uzerinden sabit Guid'e ayarlar.
    /// Domain factory overload'larini seed icin genisletmeden .http dosyalariyla tutarli kimlik saglar.
    /// </summary>
    private static void SetEntityId<TEntity>(EventDbContext db, TEntity entity, Guid id)
        where TEntity : class
    {
        db.Entry(entity).Property(EntityIdPropertyName).CurrentValue = id;
    }
}

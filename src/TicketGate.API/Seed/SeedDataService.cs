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
    /// Volkswagen Arena venue kaydini yeni SeatMap hiyerarsisiyle olusturur veya eski formattaysa gunceller.
    /// Development verisi idempotent kalir ama mevcut eski JSON formatini yeni typed SeatMap'e tasir.
    /// </summary>
    private static async Task SeedVenueAsync(EventDbContext db)
    {
        var seatMap = CreateSeedSeatMap();
        var existingVenue = await db.Venues
            .FirstOrDefaultAsync(venue => venue.Id == SeedGuids.VenueId);

        if (existingVenue is not null)
        {
            if (ShouldUpdateSeatMap(existingVenue.SeatMap, seatMap))
            {
                existingVenue.UpdateSeatMap(seatMap);
                await db.SaveChangesAsync();
                Console.WriteLine("[Seed] Venue seat map guncellendi: Volkswagen Arena");
            }

            return;
        }

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
    /// Development ortaminda kullanilacak 50 koltuklu seed SeatMap bilgisini olusturur.
    /// Ticket generation bu hiyerarsiden VIP/NORMAL/EKONOMI fiyat gruplarini uretir.
    /// </summary>
    private static SeatMap CreateSeedSeatMap()
    {
        return new SeatMap
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
    }

    /// <summary>
    /// Mevcut SeatMap'in seed hedefiyle ayni temel kapasite ve section yapisinda olup olmadigini kontrol eder.
    /// Eski rows/columns JSON formati deserialize edilince bos SeatMap'e dustugu icin otomatik guncellenir.
    /// </summary>
    private static bool ShouldUpdateSeatMap(SeatMap currentSeatMap, SeatMap seedSeatMap)
    {
        var currentSectionIds = currentSeatMap.Sections
            .Select(section => section.Id)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var seedSectionIds = seedSeatMap.Sections
            .Select(section => section.Id)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return currentSeatMap.TotalCapacity != seedSeatMap.TotalCapacity ||
            !currentSectionIds.SequenceEqual(seedSectionIds);
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

using Microsoft.EntityFrameworkCore;
using TicketGate.Event.Domain.Entities;
using TicketGate.Event.Infrastructure.Persistence;
using EventEntity = TicketGate.Event.Domain.Entities.Event;

namespace TicketGate.API.Seed;

/// <summary>
/// Development ortami seed data servisi. Uygulama baslarken test icin gerekli venue, performer ve event verilerini olusturur.
/// Veri zaten varsa tekrar eklenmez; idempotent calisir.
/// Ticket seed'i yoktur; ticket'lar manuel olarak olusturulur.
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
    /// Volkswagen Arena venue kaydini olusturur.
    /// Kayit zaten varsa tekrar eklemeden atlar.
    /// </summary>
    private static async Task SeedVenueAsync(EventDbContext db)
    {
        if (await db.Venues.AnyAsync(venue => venue.Id == SeedGuids.VenueId))
        {
            return;
        }

        var venue = Venue.Create(
            "Volkswagen Arena",
            "İstanbul, Türkiye",
            """{"rows": 10, "columns": 10}""");

        db.Venues.Add(venue);
        SetEntityId(db, venue, SeedGuids.VenueId);
        await db.SaveChangesAsync();
        Console.WriteLine("[Seed] Venue oluşturuldu: Volkswagen Arena");
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
            "Türk pop müziğinin efsanevi ismi.");

        db.Performers.Add(performer);
        SetEntityId(db, performer, SeedGuids.PerformerId);
        await db.SaveChangesAsync();
        Console.WriteLine("[Seed] Performer oluşturuldu: Tarkan");
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
        Console.WriteLine("[Seed] Event oluşturuldu: Tarkan Konseri 2026 (published)");
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

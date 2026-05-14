using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Contracts;
using TicketGate.Core.Domain;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Event.Infrastructure.Persistence;

namespace TicketGate.Event.Infrastructure;

/// <summary>
/// Event modulu seat map okuma servisidir.
/// Booking gibi diger moduller Event DbContext'e referans almadan bu contract uzerinden veri okur.
/// </summary>
internal sealed class EventSeatMapReader(EventDbContext db) : IEventSeatMapReader
{
    /// <summary>
    /// Event id ile venue seat map bilgisini projection ile okur.
    /// Cross-module DB erisimi endpoint veya handler icine sizmaz; sinir interface arkasinda kalir.
    /// </summary>
    public async Task<Result<SeatMap>> GetSeatMapByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var seatMap = await db.Events
            .AsNoTracking()
            .Where(eventEntity => eventEntity.Id == eventId)
            .Select(eventEntity => eventEntity.Venue.SeatMap)
            .SingleOrDefaultAsync(cancellationToken);

        return seatMap is null
            ? Result<SeatMap>.Fail(AppError.NotFound("Event", eventId))
            : Result<SeatMap>.Ok(seatMap);
    }
}

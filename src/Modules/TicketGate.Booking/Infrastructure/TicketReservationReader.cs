using Microsoft.EntityFrameworkCore;
using TicketGate.Booking.Domain.Enums;
using TicketGate.Booking.Infrastructure.Persistence;
using TicketGate.Core.Contracts;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Infrastructure;

/// <summary>
/// Booking modulu ticket reservation okuma servisidir.
/// Payment gibi diger moduller Booking DbContext'e direkt baglanmadan reserved ticket bilgisini bu contract ile okur.
/// </summary>
internal sealed class TicketReservationReader(BookingDbContext db) : ITicketReservationReader
{
    /// <summary>
    /// Reserved ticket ve lock sahibi kullanici bilgisini projection ile okur.
    /// Ticket available/confirmed/cancelled ise odeme akisi 409 ile durdurulur.
    /// </summary>
    public async Task<Result<TicketReservationInfo>> GetReservedTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .AsNoTracking()
            .Where(item => item.Id == ticketId && item.Status == TicketStatus.Reserved)
            .Select(item => new { item.Id, item.LockedByUserId })
            .SingleOrDefaultAsync(cancellationToken);

        if (ticket?.LockedByUserId is null)
        {
            return Result<TicketReservationInfo>.Fail(TicketReservationErrors.NotReserved(ticketId));
        }

        return Result<TicketReservationInfo>.Ok(new TicketReservationInfo(ticket.Id, ticket.LockedByUserId.Value));
    }
}

using TicketGate.Core.Errors;
using TicketGate.Core.Results;

namespace TicketGate.Core.Contracts;

/// <summary>
/// Booking modulu tarafindan saglanan ticket reservation okuma soyutlamasidir.
/// Payment modulu Booking DbContext'e referans almadan reserved ticket ve lock sahibi bilgisini bu sozlesme ile okur.
/// </summary>
public interface ITicketReservationReader
{
    /// <summary>
    /// Ticket id uzerinden reserved ticket bilgisini okur.
    /// Ticket reserved degilse veya bulunamazsa Result.Fail doner; cross-module hata akisi exception kullanmaz.
    /// </summary>
    Task<Result<TicketReservationInfo>> GetReservedTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken);
}

/// <summary>Reserved ticket, lock sahibi kullanici ve server tarafindaki ticket fiyat bilgisini tasir.</summary>
public sealed record TicketReservationInfo(Guid TicketId, Guid UserId, decimal Price);

/// <summary>
/// Ticket reservation sozlesmesinin ortak hata uretim yardimcisidir.
/// Payment ve test fake'leri ayni hata kodlarini kullanarak HTTP davranisini tutarli tutar.
/// </summary>
public static class TicketReservationErrors
{
    /// <summary>
    /// Ticket reserved durumda degilse 409 Conflict hatasi uretir.
    /// Odeme baslatma akisi ticket lock olmadan ilerlememelidir.
    /// </summary>
    public static AppError NotReserved(Guid ticketId)
    {
        return AppError.Conflict(
            "ticket.not_reserved",
            $"Ticket '{ticketId}' is not reserved.");
    }
}

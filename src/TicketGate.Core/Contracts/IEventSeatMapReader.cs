using TicketGate.Core.Domain;
using TicketGate.Core.Results;

namespace TicketGate.Core.Contracts;

/// <summary>
/// Event modulu tarafindan saglanan seat map okuma soyutlamasi.
/// Booking ticket generation akisi Event DbContext'e direkt baglanmadan venue seat map bilgisini alir.
/// </summary>
public interface IEventSeatMapReader
{
    /// <summary>
    /// Event id uzerinden ilgili venue seat map bilgisini okur.
    /// Event veya venue bulunamazsa Result hata doner; exception tabanli akis kullanilmaz.
    /// </summary>
    Task<Result<SeatMap>> GetSeatMapByEventIdAsync(Guid eventId, CancellationToken cancellationToken);
}

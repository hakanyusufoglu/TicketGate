namespace TicketGate.Booking.Infrastructure;

/// <summary>
/// Booking modulu PostgreSQL schema adini merkezi olarak tutar.
/// Migration history ve DbContext default schema ayni sabiti kullanir.
/// </summary>
internal static class BookingSchema
{
    /// <summary>
    /// Booking modulu tablolarinin tutuldugu izole PostgreSQL schema adidir.
    /// Diger modullerin tablolarina dogrudan erisimi engellemek icin kullanilir.
    /// </summary>
    public const string Name = "booking";
}

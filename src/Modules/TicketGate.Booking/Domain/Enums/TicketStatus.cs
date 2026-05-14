namespace TicketGate.Booking.Domain.Enums;

/// <summary>
/// Bilet durum gecislerini tanimlar. State machine Available'dan baslar,
/// Confirmed veya Cancelled ile sonlanir. Reserved gecici durumdur ve TTL'e tabidir.
/// </summary>
public enum TicketStatus
{
    Available,
    Reserved,
    Confirmed,
    Cancelled
}

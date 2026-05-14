namespace TicketGate.Booking.Configuration;

/// <summary>
/// Booking modulu yapilandirma ayarlari.
/// Redis lock suresi, checkout kapasitesi ve queue dispatcher parametreleri buradan okunur.
/// </summary>
public sealed class BookingSettings
{
    public const string SectionName = "BookingSettings";

    public int LockTtlSeconds { get; init; } = 600;

    public int MaxCheckoutCapacity { get; init; } = 10;

    public int QueueDispatcherIntervalSeconds { get; init; } = 5;

    public int QueueDispatchBatchSize { get; init; } = 10;
}

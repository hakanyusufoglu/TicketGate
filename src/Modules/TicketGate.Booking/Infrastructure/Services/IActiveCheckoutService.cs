namespace TicketGate.Booking.Infrastructure.Services;

/// <summary>
/// Active checkout sayaci yonetim servisi.
/// Waiting room kapasitesini dogru tutmak icin her checkout girisi ve cikisinda kullanici sahipligiyle birlikte cagrilir.
/// Sayac yalnizca bu servis uzerinden degistirilir; duplicate event ve retry kaynakli leak riski engellenir.
/// </summary>
public interface IActiveCheckoutService
{
    /// <summary>
    /// Kullanici checkout kapasitesine girince cagrilir.
    /// Redis active_checkout sayacini kullanici sahipligiyle idempotent olarak artirir.
    /// </summary>
    Task IncrementAsync(Guid eventId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Kapasite uygunsa kullaniciya checkout hakki verir.
    /// Kapasite kontrolu, kullanici sahipligi ve active_checkout artisi tek Lua script ile atomik calisir.
    /// </summary>
    Task<bool> TryIncrementWithinCapacityAsync(
        Guid eventId,
        Guid userId,
        int maxCapacity,
        CancellationToken ct = default);

    /// <summary>
    /// Waiting room'dan siradaki kullanicilara checkout hakki verir.
    /// ZPOPMIN, kullanici sahipligi ve active_checkout artisi tek Redis scriptinde yapilir.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GrantQueuedUsersAsync(
        Guid eventId,
        int maxCapacity,
        int batchSize,
        CancellationToken ct = default);

    /// <summary>
    /// Kullanici checkout kapasitesinden cikinca cagrilir.
    /// Redis active_checkout sayacini yalnizca kullanici aktif slot sahibiyse dusurur ve sifirin altina indirmez.
    /// </summary>
    Task<bool> DecrementAsync(Guid eventId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Event icin mevcut aktif checkout sayisini dondurur.
    /// Eksik Redis degeri sifir kabul edilir.
    /// </summary>
    Task<long> GetCountAsync(Guid eventId, CancellationToken ct = default);
}

namespace TicketGate.API.Seed;

/// <summary>
/// Development ortami seed verilerinin sabit Guid tanimlari.
/// .http dosyalarinda ve http-client.env.json'da bu degerler kullanilir.
/// Uygulama yeniden baslatilsa da Guid'ler degismez; test tutarliligi saglanir.
/// </summary>
public static class SeedGuids
{
    /// <summary>Volkswagen Arena venue kimligi.</summary>
    public static readonly Guid VenueId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    /// <summary>Tarkan performer kimligi.</summary>
    public static readonly Guid PerformerId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    /// <summary>Tarkan Konseri 2026 event kimligi.</summary>
    public static readonly Guid EventId =
        Guid.Parse("30000000-0000-0000-0000-000000000001");
}

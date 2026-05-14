namespace TicketGate.Core.Domain;

/// <summary>
/// Mekan koltuk haritasi. Section, row ve seat hiyerarsisini tanimlar.
/// Event modulu venue tanimi icin, Booking modulu ticket generation icin ayni contract'i kullanir.
/// </summary>
public sealed record SeatMap
{
    /// <summary>Koltuk haritasindaki fiyat ve yerlesim bolumlerini tasir.</summary>
    public IReadOnlyList<Section> Sections { get; init; } = [];

    /// <summary>
    /// Toplam koltuk kapasitesini hesaplar.
    /// Tum section'lardaki tum row'lardaki seat sayilarinin toplamidir.
    /// </summary>
    public int TotalCapacity =>
        Sections.Sum(section => section.Rows.Sum(row => row.Seats.Count));

    /// <summary>
    /// Verilen section id'sine gore fiyati doner.
    /// Section bulunamazsa null doner.
    /// </summary>
    public decimal? GetPrice(string sectionId) =>
        Sections.FirstOrDefault(section => section.Id == sectionId)?.Price;
}

/// <summary>
/// Koltuk haritasi bolumu. Ayni fiyat grubundaki row'lari ve koltuklari gruplar.
/// Her section kendi para birimi ve fiyat bilgisini tasir.
/// </summary>
public sealed record Section(
    string Id,
    string Name,
    IReadOnlyList<Row> Rows,
    decimal Price,
    string Currency = "TRY");

/// <summary>Bir bolumdeki sira tanimini ve o siradaki koltuk numaralarini tasir.</summary>
public sealed record Row(string RowCode, IReadOnlyList<int> Seats);

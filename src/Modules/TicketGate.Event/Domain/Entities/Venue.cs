using TicketGate.Core.Domain;

namespace TicketGate.Event.Domain.Entities;

/// <summary>
/// Mekan varligi. Seat map section/row/seat hiyerarsisini jsonb kolonda saklar.
/// Event olusturulunca bu harita uzerinden ticket'lar generate edilir.
/// </summary>
public sealed class Venue
{
    private Venue()
    {
    }

    private Venue(Guid id, string name, string location, SeatMap seatMap)
    {
        Id = id;
        Name = name;
        Location = location;
        SeatMap = seatMap;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Location { get; private set; } = string.Empty;

    public SeatMap SeatMap { get; private set; } = new();

    /// <summary>
    /// Yeni mekan olusturur. SeatMap zorunludur;
    /// en az bir section ve bir row icermelidir.
    /// </summary>
    public static Venue Create(string name, string location, SeatMap seatMap)
    {
        return new Venue(
            Guid.NewGuid(),
            name.Trim(),
            location.Trim(),
            seatMap);
    }
}

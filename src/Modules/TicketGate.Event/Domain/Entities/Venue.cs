namespace TicketGate.Event.Domain.Entities;

public sealed class Venue
{
    private Venue()
    {
    }

    private Venue(Guid id, string name, string location, string seatMap)
    {
        Id = id;
        Name = name;
        Location = location;
        SeatMap = seatMap;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Location { get; private set; } = string.Empty;

    public string SeatMap { get; private set; } = string.Empty;

    public static Venue Create(string name, string location, string seatMap)
    {
        return new Venue(
            Guid.NewGuid(),
            name.Trim(),
            location.Trim(),
            seatMap.Trim());
    }
}

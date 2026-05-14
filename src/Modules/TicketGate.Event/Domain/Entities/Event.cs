namespace TicketGate.Event.Domain.Entities;

public sealed class Event
{
    private Event()
    {
    }

    private Event(
        Guid id,
        string name,
        string description,
        Guid venueId,
        Guid performerId,
        DateTime startsAt,
        DateTime endsAt,
        DateTime createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        VenueId = venueId;
        PerformerId = performerId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid VenueId { get; private set; }

    public Guid PerformerId { get; private set; }

    public DateTime StartsAt { get; private set; }

    public DateTime EndsAt { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Venue Venue { get; private set; } = null!;

    public Performer Performer { get; private set; } = null!;

    public static Event Create(
        string name,
        string description,
        Guid venueId,
        Guid performerId,
        DateTime startsAt,
        DateTime endsAt)
    {
        return new Event(
            Guid.NewGuid(),
            name.Trim(),
            description.Trim(),
            venueId,
            performerId,
            startsAt,
            endsAt,
            DateTime.UtcNow);
    }

    public void Update(string name, string description, DateTime startsAt, DateTime endsAt)
    {
        Name = name.Trim();
        Description = description.Trim();
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public bool Publish()
    {
        if (IsPublished)
        {
            return false;
        }

        IsPublished = true;
        return true;
    }
}

namespace TicketGate.Event.Domain.Entities;

public sealed class Performer
{
    private Performer()
    {
    }

    private Performer(Guid id, string name, string bio)
    {
        Id = id;
        Name = name;
        Bio = bio;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Bio { get; private set; } = string.Empty;

    public static Performer Create(string name, string bio)
    {
        return new Performer(Guid.NewGuid(), name.Trim(), bio.Trim());
    }
}

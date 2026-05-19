namespace TicketGate.Event.Infrastructure.Cache;

/// <summary>
/// Event listesi output cache policy ve tag adlarini tek noktada toplar.
/// Endpoint ve host konfigurasyonu ayni sabitleri kullanarak magic string tekrarini engeller.
/// </summary>
public static class EventCachePolicies
{
    public const string Events = "events";
}


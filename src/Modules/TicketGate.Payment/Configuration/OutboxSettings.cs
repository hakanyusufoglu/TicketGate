namespace TicketGate.Payment.Configuration;

/// <summary>
/// Payment outbox worker yapilandirma ayarlari.
/// Polling araligi, batch boyutu ve retry limiti kod icinde magic number olarak tutulmaz.
/// </summary>
public sealed class OutboxSettings
{
    public const string SectionName = "OutboxSettings";

    public int PollingIntervalSeconds { get; init; } = 5;

    public int BatchSize { get; init; } = 10;

    public int MaxRetryCount { get; init; } = 3;
}

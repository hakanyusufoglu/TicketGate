namespace TicketGate.API.Configuration;

/// <summary>
/// HTTP response compression ayarlarini tasir.
/// JSON response sikistirma seviyesi appsettings uzerinden kontrol edilir.
/// </summary>
public sealed class ResponseCompressionSettings
{
    public const string SectionName = "ResponseCompression";

    public bool EnableForHttps { get; init; } = true;
}


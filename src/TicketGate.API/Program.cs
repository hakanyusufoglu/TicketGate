using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Elasticsearch;
using Prometheus;
using TicketGate.API.Extensions;
using TicketGate.API.Middleware;
using TicketGate.API.Seed;
using TicketGate.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// Serilog yapılandırması. Development'ta Console + Elasticsearch,
/// Production'da yalnızca Elasticsearch kullanılır.
/// Her log satırına CorrelationId otomatik eklenir.
/// Hassas veri loglarda kesinlikle yer almaz.
/// </summary>
var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId();

if (builder.Environment.IsDevelopment())
{
    loggerConfiguration.WriteTo.Console(new RenderedCompactJsonFormatter());
}

Log.Logger = loggerConfiguration
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(
        new Uri(builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200"))
    {
        IndexFormat = "ticketgate-logs-{0:yyyy.MM}",
        AutoRegisterTemplate = true,
        NumberOfReplicas = 0,
        NumberOfShards = 1
    })
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddTicketGateHealthChecks(builder.Configuration);
builder.Services.AddModules(builder.Configuration);
var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();

/// <summary>
/// HTTP request metriklerini otomatik toplar.
/// Status code, method ve endpoint label'lari Prometheus tarafindan scrape edilir.
/// </summary>
app.UseHttpMetrics();

app.MapModules();
app.MapTicketGateHealthChecks();

/// <summary>
/// Prometheus metrik endpoint'i. /metrics path'inde tum uygulama metriklerini Prometheus formatinda sunar.
/// Grafana bu endpoint'i Prometheus veri kaynagi uzerinden gorsellestirir.
/// </summary>
app.MapMetrics();

/// <summary>
/// Development ortamında test verilerini otomatik oluşturur.
/// Production'da çalışmaz.
/// </summary>
if (app.Environment.IsDevelopment())
{
    await SeedDataService.SeedAsync(app.Services);
}

app.Run();

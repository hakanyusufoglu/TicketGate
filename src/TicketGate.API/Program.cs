using System.IO.Compression;
using Mediator;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Elasticsearch;
using Prometheus;
using TicketGate.API.Configuration;
using TicketGate.API.Extensions;
using TicketGate.API.Middleware;
using TicketGate.API.Seed;
using TicketGate.Core.Extensions;
using TicketGate.Event.Configuration;
using TicketGate.Event.Infrastructure.Cache;

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
builder.Services.AddTicketGateCors(builder.Configuration, builder.Environment);
builder.Services.AddTicketGateRateLimiter(builder.Configuration);
builder.Services.AddTicketGateValidation();
builder.Services.Configure<ResponseCompressionSettings>(
    builder.Configuration.GetSection(ResponseCompressionSettings.SectionName));
builder.Services.Configure<EventCacheSettings>(
    builder.Configuration.GetSection(EventCacheSettings.SectionName));

/// <summary>
/// HTTP response compression. JSON response'lari sikistirarak bant genisligini azaltir.
/// SSE response'lari text/event-stream olarak bu sikistirma kapsaminda hedeflenmez.
/// </summary>
builder.Services.AddResponseCompression(options =>
{
    var compressionSettings = builder.Configuration
        .GetSection(ResponseCompressionSettings.SectionName)
        .Get<ResponseCompressionSettings>() ?? new ResponseCompressionSettings();

    options.EnableForHttps = compressionSettings.EnableForHttps;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

builder.Services.Configure<GzipCompressionProviderOptions>(
    options => options.Level = CompressionLevel.Fastest);

/// <summary>
/// Event listesi output cache policy'si.
/// Authenticated istekler cache'lenmez; publish akisi tag uzerinden temizleme yapar.
/// </summary>
builder.Services.AddOutputCache(options =>
{
    var eventCacheSettings = builder.Configuration
        .GetSection(EventCacheSettings.SectionName)
        .Get<EventCacheSettings>() ?? new EventCacheSettings();

    options.AddPolicy(EventCachePolicies.Events, policy =>
        policy
            .Expire(TimeSpan.FromSeconds(eventCacheSettings.EventListOutputCacheTtlSeconds))
            .Tag(EventCachePolicies.Events)
            .SetVaryByHeader("Accept-Language"));
});

builder.Services.AddModules(builder.Configuration);
builder.Services.AddMediator(options =>
    options.ServiceLifetime = ServiceLifetime.Scoped);

var app = builder.Build();
app.UseResponseCompression();
app.UseRouting();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseTicketGateCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();

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

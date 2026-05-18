using Confluent.Kafka;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace TicketGate.API.Extensions;

/// <summary>
/// Uygulama sağlık kontrolü yapılandırması.
/// /health/live process durumunu, /health/ready bağımlılık hazırlığını,
/// /health genel durumu Docker Compose ve izleme araçları için sunar.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// PostgreSQL, Redis, Kafka ve Elasticsearch bağımlılıklarını readiness etiketiyle kaydeder.
    /// Liveness endpoint'i bağımlılık çalıştırmadan yalnızca uygulama process durumunu raporlar.
    /// </summary>
    public static IServiceCollection AddTicketGateHealthChecks(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                config.GetConnectionString("Booking")!,
                name: "postgres",
                tags: ["ready"])
            .AddRedis(
                config.GetConnectionString("Redis")!,
                name: "redis",
                tags: ["ready"])
            .AddKafka(
                new ProducerConfig
                {
                    BootstrapServers = config["Kafka:BootstrapServers"]
                },
                name: "kafka",
                tags: ["ready"])
            .AddElasticsearch(
                config["Elasticsearch:Uri"]!,
                name: "elasticsearch",
                tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Liveness, readiness ve genel health endpoint'lerini JSON response writer ile map eder.
    /// Readiness yalnızca ready tag'li bağımlılık kontrollerini çalıştırır.
    /// </summary>
    public static WebApplication MapTicketGateHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}

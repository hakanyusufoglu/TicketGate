using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace TicketGate.TestInfrastructure;

/// <summary>
/// Tüm integration testleri için ortak base sınıf.
/// Her test çalışması için gerçek PostgreSQL ve Redis container'ı başlatır.
/// InMemory DB xmin concurrency token'ını ve Redis SETNX atomikliğini desteklemez; bu nedenle Testcontainers zorunludur.
/// Respawn ile her test sonrası DB temizlenir ve tam izolasyon sağlanır.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private RedisContainer? _redis;
    private Respawner? _respawner;
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Testlerin servis sağlayıcısına erişmesini sağlar.
    /// Modül testleri handler, DbContext veya Redis bağımlılıklarını bu provider üzerinden çözer.
    /// </summary>
    protected IServiceProvider Services { get; private set; } = default!;

    /// <summary>
    /// MediatR sender erişimini modül integration testlerine sağlar.
    /// Modül kendi MediatR kayıtlarını eklediğinde handler akışları doğrudan bu property üzerinden çağrılır.
    /// </summary>
    protected ISender Sender => Services.GetRequiredService<ISender>();

    /// <summary>
    /// PostgreSQL container bağlantı bilgisini modül testlerine açar.
    /// DbContext kayıtları gerçek PostgreSQL üzerinde çalışmak için bu değeri kullanır.
    /// </summary>
    protected string PostgresConnectionString => _postgres?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container başlatılmadan bağlantı bilgisi kullanılamaz.");

    /// <summary>
    /// Redis container bağlantı bilgisini modül testlerine açar.
    /// Redis lock, sorted set ve pub/sub davranışları gerçek Redis üzerinde bu bağlantıyla test edilir.
    /// </summary>
    protected string RedisConnectionString => _redis?.GetConnectionString()
        ?? throw new InvalidOperationException("Redis container başlatılmadan bağlantı bilgisi kullanılamaz.");

    /// <summary>
    /// PostgreSQL ve Redis container'larını başlatır, servis sağlayıcıyı kurar ve Respawn checkpoint'ini oluşturur.
    /// Her integration test örneği için gerçek altyapı hazırlandığından InMemory kaynaklara düşülmez.
    /// </summary>
    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("ticketgate_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        _redis = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCommand("redis-server", "--notify-keyspace-events", "KEx")
            .Build();

        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        await EnsureSchemasAsync();

        var services = new ServiceCollection();
        ConfigureServices(services);
        services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(RedisConnectionString));

        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;

        await ApplyMigrationsAsync(Services);

        await using var connection = new NpgsqlConnection(PostgresConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["booking", "payment"]
        });
    }

    /// <summary>
    /// Test veritabanında modül şemalarını ve Respawn marker tablolarını oluşturur.
    /// Migration bulunmayan erken fazlarda Respawn'ın boş şemada kırılmasını engeller.
    /// </summary>
    private async Task EnsureSchemasAsync()
    {
        await using var connection = new NpgsqlConnection(PostgresConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            create schema if not exists booking;
            create schema if not exists payment;
            create table if not exists booking.__respawn_marker (id integer primary key);
            create table if not exists payment.__respawn_marker (id integer primary key);
            """;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Modül testlerinin kendi migration akışını çalıştırması için genişletme noktası sağlar.
    /// Booking ve Payment DbContext'leri eklendiğinde ilgili migration çağrıları burada override edilir.
    /// </summary>
    protected virtual Task ApplyMigrationsAsync(IServiceProvider services)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Her testten önce çağrılır.
    /// Respawn ile PostgreSQL verisini temizler, Redis'i FLUSHDB ile sıfırlar ve test izolasyonunu korur.
    /// </summary>
    protected async Task ResetAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawn checkpoint'i oluşturulmadan reset çalıştırılamaz.");
        }

        await using var connection = new NpgsqlConnection(PostgresConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);

        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        await redis.GetDatabase().ExecuteAsync("FLUSHDB");
    }

    /// <summary>
    /// Her test class'ının kendi servis konfigürasyonunu eklemesi için kullanılır.
    /// DbContext, MediatR, validator ve modüle özel dış bağımlılıklar burada register edilir.
    /// </summary>
    protected abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// ServiceProvider ve container kaynaklarını serbest bırakır.
    /// Testcontainers kaynakları kapatıldığından PostgreSQL ve Redis süreçleri test sonunda yaşamaz.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }

        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }
    }
}

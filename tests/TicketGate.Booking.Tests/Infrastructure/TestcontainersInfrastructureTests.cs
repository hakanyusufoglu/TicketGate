using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;
using TicketGate.Booking.Tests;

namespace TicketGate.Booking.Tests.Infrastructure;

/// <summary>
/// Booking integration test altyapısının gerçek PostgreSQL ve Redis container'larıyla çalıştığını doğrular.
/// xmin concurrency ve Redis SETNX testleri bu altyapıya dayanacağı için bu smoke test kritik tabanı kontrol eder.
/// </summary>
public sealed class TestcontainersInfrastructureTests : BookingIntegrationTestBase
{
    /// <summary>
    /// PostgreSQL container'ına bağlanılabildiğini ve booking şemasının test çalışması için hazır olduğunu doğrular.
    /// InMemory DB kullanılmadığını kanıtladığı için Booking concurrency testlerinden önce kritik güvence sağlar.
    /// </summary>
    [Fact]
    public async Task ResetAsync_PostgreSqlContainer_IsReachable()
    {
        await ResetAsync();

        await using var connection = new NpgsqlConnection(PostgresConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand("select exists(select 1 from information_schema.schemata where schema_name = 'booking')", connection);
        var exists = (bool?)await command.ExecuteScalarAsync();

        exists.Should().BeTrue();
    }

    /// <summary>
    /// Redis container'ında SET NX kilit semantiğinin gerçek Redis üzerinde çalıştığını doğrular.
    /// Rezervasyon race condition testleri Redis atomikliğine bağlı olduğu için sahte cache kullanılmamalıdır.
    /// </summary>
    [Fact]
    public async Task ResetAsync_RedisContainer_SupportsSetNxAndFlushesData()
    {
        await ResetAsync();

        var redis = Services.GetRequiredService<IConnectionMultiplexer>();
        var database = redis.GetDatabase();
        var key = $"ticket:{Guid.NewGuid()}:lock";

        var firstSet = await database.StringSetAsync(key, "user-1", TimeSpan.FromMinutes(10), When.NotExists);
        var secondSet = await database.StringSetAsync(key, "user-2", TimeSpan.FromMinutes(10), When.NotExists);

        firstSet.Should().BeTrue();
        secondSet.Should().BeFalse();

        await ResetAsync();

        var valueAfterReset = await database.StringGetAsync(key);
        valueAfterReset.HasValue.Should().BeFalse();
    }
}

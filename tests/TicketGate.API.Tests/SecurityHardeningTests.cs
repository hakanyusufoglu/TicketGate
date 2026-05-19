using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using TicketGate.API.Extensions;
using TicketGate.API.Middleware;
using TicketGate.Core.Security;

namespace TicketGate.API.Tests;

/// <summary>
/// API host seviyesindeki guvenlik middleware'lerini dogrular.
/// Rate limit, CORS ve response header davranisi module handler'larindan bagimsiz test edilir.
/// </summary>
public sealed class SecurityHardeningTests
{
    /// <summary>
    /// Auth policy limitini asan istegin 429 ve standart problem payload'u dondugunu dogrular.
    /// Bu test brute force korumasinin endpoint metadata'si uzerinden calistigini kanitlar.
    /// </summary>
    [Fact]
    public async Task Auth_policy_returns_429_after_configured_limit()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.WebHost.UseTestServer();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Enabled"] = "true",
                ["RateLimiting:Auth:PermitLimit"] = "10",
                ["RateLimiting:Auth:WindowSeconds"] = "60",
                ["RateLimiting:Auth:QueueLimit"] = "0"
            })
            .Build();
        builder.Services.AddTicketGateRateLimiter(config);

        await using var app = builder.Build();
        app.UseRateLimiter();
        app.MapPost("/api/v1/auth/login", () => Results.Ok())
            .RequireRateLimiting(RateLimitPolicies.Auth);

        await app.StartAsync();
        var client = app.GetTestClient();

        for (var i = 0; i < 10; i++)
        {
            var response = await client.PostAsync("/api/v1/auth/login", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var rejected = await client.PostAsync("/api/v1/auth/login", null);

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var payload = await rejected.Content.ReadAsStringAsync();
        payload.Should().Contain("RateLimit.Exceeded");
    }

    /// <summary>
    /// SecurityHeadersMiddleware'in her response'a temel hardening header'larini ekledigini dogrular.
    /// Health endpoint'i is mantigi icermedigi icin middleware davranisini izole eder.
    /// </summary>
    [Fact]
    public async Task Security_headers_are_added_to_every_response()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.WebHost.UseTestServer();

        await using var app = builder.Build();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.MapGet("/health", () => Results.Ok());

        await app.StartAsync();
        var response = await app.GetTestClient().GetAsync("/health");

        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
        response.Headers.GetValues("X-XSS-Protection").Should().Contain("1; mode=block");
    }

    /// <summary>
    /// Development CORS politikasinin herhangi bir origin'e izin verdigini dogrular.
    /// Production allowlist'i ayri policy oldugundan lokal gelistirme akisi kisitlanmaz.
    /// </summary>
    [Fact]
    public async Task Development_cors_allows_any_origin()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddTicketGateCors(new ConfigurationBuilder().Build(), builder.Environment);

        await using var app = builder.Build();
        app.UseTicketGateCors();
        app.MapGet("/health", () => Results.Ok());

        await app.StartAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://local.ticketgate.test");

        var response = await app.GetTestClient().SendAsync(request);

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Contain("*");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TicketGate.Identity.Infrastructure.Persistence;

namespace TicketGate.Identity.Tests;

internal static class IdentityTestFactory
{
    public static IdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new IdentityDbContext(options);
    }

    public static IConfiguration CreateJwtConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "TicketGate.Tests",
                ["Jwt:Audience"] = "TicketGate.Tests.Clients",
                ["Jwt:SecretKey"] = "TicketGateTestsSecretKeyWithAtLeastThirtyTwoCharacters"
            })
            .Build();
    }
}

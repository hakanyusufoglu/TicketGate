using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TicketGate.Identity.Configuration;
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

    public static IOptions<JwtSettings> CreateJwtOptions()
    {
        return Options.Create(new JwtSettings
        {
            Issuer = "TicketGate.Tests",
            Audience = "TicketGate.Tests.Clients",
            SecretKey = "TicketGateTestsSecretKeyWithAtLeastThirtyTwoCharacters"
        });
    }
}

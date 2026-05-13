using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Identity.Domain.Entities;
using TicketGate.Identity.Features.Auth.Commands.RefreshToken;
using TicketGate.Identity.Infrastructure.Persistence;

namespace TicketGate.Identity.Tests;

public sealed class RefreshTokenHandlerTests
{
    [Fact]
    public async Task Handle_ValidToken_RotatesAndReturnsNewTokens()
    {
        await using var db = IdentityTestFactory.CreateDbContext();
        var refreshToken = await SeedRefreshTokenAsync(
            db,
            "active-token",
            DateTime.UtcNow.AddDays(7),
            revoke: false,
            CancellationToken.None);
        var handler = new RefreshTokenHandler(db, IdentityTestFactory.CreateJwtConfiguration());

        var result = await handler.Handle(new RefreshTokenCommand("active-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBe("active-token");
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        refreshToken.IsRevoked.Should().BeTrue();
        refreshToken.RevokedAt.Should().NotBeNull();
        var tokens = await db.RefreshTokens.ToListAsync(CancellationToken.None);
        tokens.Should().HaveCount(2);
        tokens.Should().Contain(token => token.Token == result.Value.RefreshToken && token.IsActive);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsUnauthorized()
    {
        await using var db = IdentityTestFactory.CreateDbContext();
        await SeedRefreshTokenAsync(
            db,
            "expired-token",
            DateTime.UtcNow.AddDays(-1),
            revoke: false,
            CancellationToken.None);
        var handler = new RefreshTokenHandler(db, IdentityTestFactory.CreateJwtConfiguration());

        var result = await handler.Handle(new RefreshTokenCommand("expired-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Unauthorized);
        result.Error.Message.Should().Be("Invalid or expired token.");
        (await db.RefreshTokens.CountAsync(CancellationToken.None)).Should().Be(1);
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsUnauthorized()
    {
        await using var db = IdentityTestFactory.CreateDbContext();
        await SeedRefreshTokenAsync(
            db,
            "revoked-token",
            DateTime.UtcNow.AddDays(7),
            revoke: true,
            CancellationToken.None);
        var handler = new RefreshTokenHandler(db, IdentityTestFactory.CreateJwtConfiguration());

        var result = await handler.Handle(new RefreshTokenCommand("revoked-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Unauthorized);
        result.Error.Message.Should().Be("Invalid or expired token.");
        (await db.RefreshTokens.CountAsync(CancellationToken.None)).Should().Be(1);
    }

    private static async Task<RefreshToken> SeedRefreshTokenAsync(
        IdentityDbContext db,
        string token,
        DateTime expiresAt,
        bool revoke,
        CancellationToken cancellationToken)
    {
        var user = User.Create(
            "refresh@ticketgate.test",
            BCrypt.Net.BCrypt.HashPassword("P@ssword123"),
            "Refresh User");
        var refreshToken = RefreshToken.Create(user.Id, token, expiresAt);

        if (revoke)
        {
            refreshToken.Revoke();
        }

        await db.Users.AddAsync(user, cancellationToken);
        await db.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }
}

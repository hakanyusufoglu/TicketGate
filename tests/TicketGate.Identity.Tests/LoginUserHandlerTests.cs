using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Identity.Domain.Entities;
using TicketGate.Identity.Features.Auth.Commands.LoginUser;
using TicketGate.Identity.Infrastructure.Persistence;

namespace TicketGate.Identity.Tests;

public sealed class LoginUserHandlerTests
{
    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens()
    {
        await using var db = IdentityTestFactory.CreateDbContext();
        await SeedUserAsync(db, "login@ticketgate.test", "P@ssword123", CancellationToken.None);
        var handler = new LoginUserHandler(db, IdentityTestFactory.CreateJwtOptions());
        var command = new LoginUserCommand("login@ticketgate.test", "P@ssword123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        var refreshToken = await db.RefreshTokens.SingleAsync(CancellationToken.None);
        refreshToken.Token.Should().Be(result.Value.RefreshToken);
        refreshToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsUnauthorized()
    {
        await using var db = IdentityTestFactory.CreateDbContext();
        await SeedUserAsync(db, "wrong-password@ticketgate.test", "P@ssword123", CancellationToken.None);
        var handler = new LoginUserHandler(db, IdentityTestFactory.CreateJwtOptions());
        var command = new LoginUserCommand("wrong-password@ticketgate.test", "wrong-password");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Unauthorized);
        result.Error.Message.Should().Be("Invalid credentials.");
        (await db.RefreshTokens.CountAsync(CancellationToken.None)).Should().Be(0);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsUnauthorized()
    {
        await using var db = IdentityTestFactory.CreateDbContext();
        var handler = new LoginUserHandler(db, IdentityTestFactory.CreateJwtOptions());
        var command = new LoginUserCommand("missing@ticketgate.test", "P@ssword123");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Unauthorized);
        result.Error.Message.Should().Be("Invalid credentials.");
        (await db.RefreshTokens.CountAsync(CancellationToken.None)).Should().Be(0);
    }

    private static async Task<User> SeedUserAsync(
        IdentityDbContext db,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = User.Create(email, BCrypt.Net.BCrypt.HashPassword(password), "Login User");

        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return user;
    }
}

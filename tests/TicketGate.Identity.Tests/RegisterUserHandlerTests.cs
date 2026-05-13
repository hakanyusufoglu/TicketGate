using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Identity.Features.Auth.Commands.RegisterUser;
using TicketGate.Identity.Infrastructure.Persistence;

namespace TicketGate.Identity.Tests;

public sealed class RegisterUserHandlerTests
{
    [Fact]
    public async Task Handle_NewUser_ReturnsOkWithUserId()
    {
        await using var db = IdentityTestFactory.CreateDbContext();
        var handler = new RegisterUserHandler(db);
        var command = new RegisterUserCommand("new.user@ticketgate.test", "P@ssword123", "New User");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().NotBeEmpty();

        var user = await db.Users.SingleAsync(CancellationToken.None);
        user.Email.Should().Be(command.Email);
        BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflict()
    {
        await using var db = IdentityTestFactory.CreateDbContext();
        await SeedUserAsync(db, "duplicate@ticketgate.test", CancellationToken.None);
        var handler = new RegisterUserHandler(db);
        var command = new RegisterUserCommand("duplicate@ticketgate.test", "P@ssword123", "Duplicate User");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(AppErrorType.Conflict);
        result.Error.Code.Should().Be("User.EmailTaken");
        (await db.Users.CountAsync(CancellationToken.None)).Should().Be(1);
    }

    private static async Task SeedUserAsync(IdentityDbContext db, string email, CancellationToken cancellationToken)
    {
        var user = Domain.Entities.User.Create(
            email,
            BCrypt.Net.BCrypt.HashPassword("P@ssword123"),
            "Existing User");

        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Identity.Domain.Entities;
using TicketGate.Identity.Infrastructure.Persistence;

namespace TicketGate.Identity.Features.Auth.Commands.RegisterUser;

public sealed class RegisterUserHandler(IdentityDbContext db)
    : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await db.Users.AnyAsync(user => user.Email == email, cancellationToken);

        if (emailTaken)
        {
            return Result<RegisterUserResponse>.Fail(
                AppError.Conflict("User.EmailTaken", "Email address is already registered."));
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.Create(email, passwordHash, request.FullName);

        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<RegisterUserResponse>.Ok(new RegisterUserResponse(user.Id));
    }
}

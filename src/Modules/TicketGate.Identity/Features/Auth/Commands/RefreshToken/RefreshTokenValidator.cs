using FluentValidation;

namespace TicketGate.Identity.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty();
    }
}

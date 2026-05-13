using FluentValidation;

namespace TicketGate.Identity.Features.Auth.Commands.LoginUser;

public sealed class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(command => command.Password)
            .NotEmpty();
    }
}

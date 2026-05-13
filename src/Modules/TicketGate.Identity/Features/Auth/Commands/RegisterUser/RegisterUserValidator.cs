using FluentValidation;

namespace TicketGate.Identity.Features.Auth.Commands.RegisterUser;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(100);
    }
}

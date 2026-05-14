using FluentValidation;

namespace TicketGate.Event.Features.Events.Commands.UpdateEvent;

public sealed class UpdateEventValidator : AbstractValidator<UpdateEventCommand>
{
    public UpdateEventValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(1000);

        RuleFor(command => command)
            .Must(command => command.StartsAt < command.EndsAt)
            .WithMessage("StartsAt must be less than EndsAt.");
    }
}

using FluentValidation;

namespace TicketGate.Event.Features.Events.Commands.CreateEvent;

public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(1000);

        RuleFor(command => command.VenueId)
            .NotEmpty();

        RuleFor(command => command.PerformerId)
            .NotEmpty();

        RuleFor(command => command.StartsAt)
            .Must(startsAt => startsAt > DateTime.UtcNow)
            .WithMessage("StartsAt must be greater than current UTC time.");

        RuleFor(command => command)
            .Must(command => command.EndsAt > command.StartsAt)
            .WithMessage("EndsAt must be greater than StartsAt.");
    }
}

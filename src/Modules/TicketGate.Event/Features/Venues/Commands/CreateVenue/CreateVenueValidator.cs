using FluentValidation;

namespace TicketGate.Event.Features.Venues.Commands.CreateVenue;

public sealed class CreateVenueValidator : AbstractValidator<CreateVenueCommand>
{
    public CreateVenueValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Location)
            .NotEmpty()
            .MaximumLength(500);
    }
}

using FluentValidation;

namespace TicketGate.Event.Features.Venues.Commands.CreateVenue;

/// <summary>CreateVenue komutu icin isim, lokasyon ve SeatMap zorunluluklarini dogrular.</summary>
public sealed class CreateVenueValidator : AbstractValidator<CreateVenueCommand>
{
    /// <summary>
    /// Mekan input kurallarini tanimlar.
    /// SeatMap en az bir section ve toplamda en az bir koltuk icermelidir.
    /// </summary>
    public CreateVenueValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Location)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(command => command.SeatMap)
            .NotNull();

        RuleFor(command => command.SeatMap.Sections)
            .NotEmpty();

        RuleFor(command => command.SeatMap.TotalCapacity)
            .GreaterThan(0);
    }
}

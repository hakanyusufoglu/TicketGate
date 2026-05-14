using FluentValidation;

namespace TicketGate.Booking.Features.Tickets.Commands.GenerateTickets;

/// <summary>GenerateTickets komutu icin EventId ve SeatMap kurallarini dogrular.</summary>
public sealed class GenerateTicketsValidator : AbstractValidator<GenerateTicketsCommand>
{
    /// <summary>
    /// Ticket generation icin event id ve dolu SeatMap zorunlulugunu tanimlar.
    /// Bos harita bulk insert'in sessizce sifir bilet uretmesini engeller.
    /// </summary>
    public GenerateTicketsValidator()
    {
        RuleFor(command => command.EventId)
            .NotEmpty();

        RuleFor(command => command.SeatMap)
            .NotNull();

        RuleFor(command => command.SeatMap.Sections)
            .NotEmpty();

        RuleFor(command => command.SeatMap.TotalCapacity)
            .GreaterThan(0);
    }
}

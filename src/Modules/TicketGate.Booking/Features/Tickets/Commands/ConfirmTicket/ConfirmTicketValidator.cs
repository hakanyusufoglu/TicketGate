using FluentValidation;

namespace TicketGate.Booking.Features.Tickets.Commands.ConfirmTicket;

/// <summary>ConfirmTicket komutu icin dogrulama kurallari.</summary>
internal sealed class ConfirmTicketValidator : AbstractValidator<ConfirmTicketCommand>
{
    /// <summary>
    /// TicketId ve UserId alanlarinin bos Guid olmamasini zorunlu kilar.
    /// Onay akisinda Redis lock sahibi bu kullanici id ile karsilastirilir.
    /// </summary>
    public ConfirmTicketValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}

using FluentValidation;

namespace TicketGate.Booking.Features.Tickets.Commands.CancelTicket;

/// <summary>CancelTicket komutu icin dogrulama kurallari.</summary>
internal sealed class CancelTicketValidator : AbstractValidator<CancelTicketCommand>
{
    /// <summary>
    /// TicketId ve UserId alanlarinin bos Guid olmamasini zorunlu kilar.
    /// Iptal yetki kontrolu ileride auth claim'leriyle bu kullanici bilgisi uzerinden genisletilebilir.
    /// </summary>
    public CancelTicketValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}

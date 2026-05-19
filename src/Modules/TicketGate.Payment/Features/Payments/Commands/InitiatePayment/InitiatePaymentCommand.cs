using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Payment.Features.Payments.Commands.InitiatePayment;

/// <summary>Odeme baslatma istegi. UserId endpoint katmaninda JWT claim'den okunur ve handler HTTP bagimsiz kalir.</summary>
public sealed record InitiatePaymentCommand(
    Guid TicketId,
    Guid UserId,
    string Provider,
    string IdempotencyKey) : IRequest<Result<InitiatePaymentResponse>>;

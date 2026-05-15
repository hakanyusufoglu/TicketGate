using MediatR;
using TicketGate.Core.Results;

namespace TicketGate.Payment.Features.Payments.Commands.InitiatePayment;

/// <summary>Odeme baslatma istegi. IdempotencyKey client retry'larinda ayni response'u dondurmek icin kullanilir.</summary>
public sealed record InitiatePaymentCommand(
    Guid TicketId,
    Guid UserId,
    decimal Amount,
    string Provider,
    string IdempotencyKey) : IRequest<Result<InitiatePaymentResponse>>;

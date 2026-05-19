using Mediator;
using TicketGate.Core.Results;

namespace TicketGate.Payment.Features.Payments.Queries.GetPaymentById;

/// <summary>Id ile tekil odeme sorgulama.</summary>
public sealed record GetPaymentByIdQuery(Guid Id) : IRequest<Result<PaymentDetailDto>>;

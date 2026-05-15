using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using TicketGate.Payment.Infrastructure.Persistence;

namespace TicketGate.Payment.Features.Payments.Queries.GetPaymentById;

/// <summary>
/// Id ile tekil odeme sorgular.
/// AsNoTracking ve Select projection kullanarak gereksiz entity tracking olusturmaz.
/// </summary>
internal sealed class GetPaymentByIdHandler(PaymentDbContext db)
    : IRequestHandler<GetPaymentByIdQuery, Result<PaymentDetailDto>>
{
    /// <summary>
    /// Payment detayini projection ile okur ve bulunamazsa 404 doner.
    /// Query handler validator kullanmaz; yalnizca okuma akisini yurutur.
    /// </summary>
    public async Task<Result<PaymentDetailDto>> Handle(
        GetPaymentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await db.Payments
            .AsNoTracking()
            .Where(item => item.Id == request.Id)
            .Select(item => new PaymentDetailDto(
                item.Id,
                item.TicketId,
                item.Amount,
                item.Currency,
                item.Status.ToString(),
                item.CreatedAt,
                item.CompletedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return payment is null
            ? Result<PaymentDetailDto>.Fail(AppError.NotFound("Payment", request.Id))
            : Result<PaymentDetailDto>.Ok(payment);
    }
}

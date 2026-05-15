using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TicketGate.Core.Extensions;
using TicketGate.Payment.Features.Payments.Commands.InitiatePayment;
using TicketGate.Payment.Features.Payments.Commands.RefundPayment;
using TicketGate.Payment.Features.Payments.Queries.GetPaymentById;

namespace TicketGate.Payment.Features.Payments.Endpoints;

/// <summary>
/// Odeme islemleri HTTP endpoint'leri.
/// Tum is mantigi handler'larda; endpoint sadece HTTP request'i command/query nesnesine cevirir.
/// </summary>
public static class PaymentEndpoints
{
    /// <summary>
    /// Payment endpoint'lerini /api/v1/payments grubu altinda kaydeder.
    /// Basarili initiate 201, refund 204, query 200 donuslerini Result extension uzerinden uretir.
    /// </summary>
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payments").WithTags("Payments");

        group.MapPost("/initiate", async (
            InitiatePaymentCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        });

        group.MapPost("/{id:guid}/refund", async (
            Guid id,
            RefundPaymentRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RefundPaymentCommand(id, request.UserId), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status204NoContent);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPaymentByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        });

        return app;
    }

    /// <summary>
    /// Iade endpoint'i icin kullanici id request govdesidir.
    /// Gateway auth tamamlanana kadar kullanici bilgisi bu alan uzerinden iletilir.
    /// </summary>
    private sealed record RefundPaymentRequest(Guid UserId);
}

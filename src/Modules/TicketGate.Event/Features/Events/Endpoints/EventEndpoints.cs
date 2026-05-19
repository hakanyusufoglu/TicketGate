using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TicketGate.Core.Extensions;
using TicketGate.Core.Pagination;
using TicketGate.Core.Security;
using TicketGate.Event.Features.Events.Commands.CreateEvent;
using TicketGate.Event.Features.Events.Commands.PublishEvent;
using TicketGate.Event.Features.Events.Commands.UpdateEvent;
using TicketGate.Event.Features.Events.Queries.GetEventById;
using TicketGate.Event.Features.Events.Queries.GetEventList;
using TicketGate.Event.Features.Performers.Commands.CreatePerformer;
using TicketGate.Event.Features.Venues.Commands.CreateVenue;
using TicketGate.Event.Features.Venues.Queries.GetVenueById;

namespace TicketGate.Event.Features.Events.Endpoints;

/// <summary>
/// Event, venue ve performer HTTP endpoint'lerini kaydeder.
/// Endpoint katmani yalnizca HTTP request bilgisini command/query nesnelerine cevirir.
/// </summary>
public static class EventEndpoints
{
    /// <summary>
    /// Event modulu endpoint'lerini /api/v1 grubu altinda yayinlar.
    /// Is kurallari handler'larda kalir; burada sadece HTTP sonucu Result extension ile uretilir.
    /// </summary>
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Events");

        group.MapPost("/events", async (
            CreateEventCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
            .WithName("CreateEvent")
            .WithSummary("Event olusturur")
            .WithDescription("""
                Yeni event kaydi olusturur.
                Venue ve performer referanslari command validator ve handler akisiyle dogrulanir.
                """)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .RequireRateLimiting(RateLimitPolicies.Reserve);

        group.MapPut("/events/{id:guid}", async (
            Guid id,
            UpdateEventRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new UpdateEventCommand(id, request.Name, request.Description, request.StartsAt, request.EndsAt),
                cancellationToken);

            return result.ToHttpResult(StatusCodes.Status204NoContent);
        })
            .WithName("UpdateEvent")
            .WithSummary("Event gunceller")
            .WithDescription("""
                Mevcut event bilgisini gunceller.
                Tarih ve temel alan dogrulamalari command validator tarafindan yapilir.
                """)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/events/{id:guid}/publish", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new PublishEventCommand(id), cancellationToken);
            return result.ToHttpResult(StatusCodes.Status204NoContent);
        })
            .WithName("PublishEvent")
            .WithSummary("Event yayinlar")
            .WithDescription("""
                Event'i yayinlanmis duruma gecirir.
                Zaten yayinlanmis event icin conflict sonucu doner.
                """)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapGet("/events/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetEventByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
            .WithName("GetEventById")
            .WithSummary("Event detayini getirir")
            .WithDescription("""
                Id ile tekil event detayini getirir.
                Venue ve performer bilgileri projection-first okunur.
                """)
            .Produces<EventDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireRateLimiting(RateLimitPolicies.Read);

        group.MapGet("/events", async (
            ISender sender,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? city = null,
            DateTime? startsAfter = null) =>
        {
            var result = await sender.Send(
                new GetEventListQuery(page, pageSize, search, city, startsAfter),
                cancellationToken);

            return result.ToHttpResult();
        })
            .WithName("GetEvents")
            .WithSummary("Event listesini getirir")
            .WithDescription("""
                Yayinlanmis event listesini sayfalama, arama, sehir ve baslangic tarihi filtreleriyle getirir.
                Query handler projection-first okuma yapar.
            """)
            .Produces<PagedResult<EventListDto>>(StatusCodes.Status200OK)
            .RequireRateLimiting(RateLimitPolicies.Read);

        group.MapPost("/venues", async (
            CreateVenueCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
            .WithName("CreateVenue")
            .WithSummary("Venue olusturur")
            .WithDescription("""
                Yeni venue kaydi olusturur.
                SeatMap bilgisi typed Core contract modeliyle alinir.
                """)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/venues/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetVenueByIdQuery(id), cancellationToken);
            return result.ToHttpResult();
        })
            .WithName("GetVenueById")
            .WithSummary("Venue detayini getirir")
            .WithDescription("""
                Id ile tekil venue detayini getirir.
                SeatMap typed model olarak response'a yansitilir.
                """)
            .Produces<VenueDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/performers", async (
            CreatePerformerCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(StatusCodes.Status201Created);
        })
            .WithName("CreatePerformer")
            .WithSummary("Performer olusturur")
            .WithDescription("""
                Yeni performer kaydi olusturur.
                Temel alan dogrulamalari command validator tarafindan yapilir.
                """)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    /// <summary>Event guncelleme endpoint'i icin HTTP request modelidir.</summary>
    private sealed record UpdateEventRequest(
        string Name,
        string Description,
        DateTime StartsAt,
        DateTime EndsAt);
}

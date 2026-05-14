using Microsoft.AspNetCore.Http;
using TicketGate.Core.Errors;
using TicketGate.Core.Extensions;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Tests;

public sealed class ResultExtensionsTests
{
    [Fact]
    public void NonGenericResult_ToHttpResult_ReturnsConfiguredSuccessStatus()
    {
        var httpResult = Result.Ok().ToHttpResult(StatusCodes.Status204NoContent);

        var statusCodeHttpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status204NoContent, statusCodeHttpResult.StatusCode);
    }

    [Fact]
    public void ValidationFailure_ToHttpResult_ReturnsUnprocessableEntity()
    {
        var result = Result<Guid>.Fail(AppError.Validation("validation.failed", "Invalid request."));

        var httpResult = result.ToHttpResult();

        var statusCodeHttpResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(httpResult);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, statusCodeHttpResult.StatusCode);
    }
}

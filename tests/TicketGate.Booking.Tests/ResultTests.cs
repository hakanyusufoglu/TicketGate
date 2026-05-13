using TicketGate.Core.Errors;
using TicketGate.Core.Results;

namespace TicketGate.Booking.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Ok_ShouldCreateSuccessfulResultWithValue()
    {
        var ticketId = Guid.NewGuid();

        var result = Result<Guid>.Ok(ticketId);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ticketId, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_ShouldCreateFailedResultWithError()
    {
        var ticketId = Guid.NewGuid();
        var error = AppError.TicketAlreadyLocked(ticketId);

        var result = Result<Guid>.Fail(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(default, result.Value);
        Assert.Equal(error, result.Error);
        Assert.Equal(AppErrorType.Conflict, result.Error?.Type);
    }

    [Fact]
    public void NonGenericFail_ShouldCreateFailedResultWithoutValue()
    {
        var error = AppError.ConcurrencyConflict();

        var result = Result.Fail(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }
}

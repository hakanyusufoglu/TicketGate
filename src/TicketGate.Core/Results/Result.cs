using TicketGate.Core.Errors;

namespace TicketGate.Core.Results;

public class Result
{
    protected Result(bool isSuccess, AppError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public AppError? Error { get; }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public static Result Ok()
    {
        return new Result(true, null);
    }

    public static Result Fail(AppError error)
    {
        return new Result(false, error);
    }
}

public sealed class Result<T> : Result
{
    private Result(T? value, bool isSuccess, AppError? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Ok(T value)
    {
        return new Result<T>(value, true, null);
    }

    public static new Result<T> Fail(AppError error)
    {
        return new Result<T>(default, false, error);
    }
}

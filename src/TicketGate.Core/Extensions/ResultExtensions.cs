using Microsoft.AspNetCore.Http;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace TicketGate.Core.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result, int successCode = StatusCodes.Status204NoContent)
    {
        if (result.IsSuccess)
        {
            return HttpResults.StatusCode(successCode);
        }

        var error = result.Error ?? new AppError(
            AppErrorType.Internal,
            "internal.unexpected_error",
            "An unexpected error occurred.");

        return ToProblemResult(error);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, int successCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successCode == StatusCodes.Status204NoContent
                ? HttpResults.StatusCode(StatusCodes.Status204NoContent)
                : HttpResults.Json(result.Value, statusCode: successCode);
        }

        var error = result.Error ?? new AppError(
            AppErrorType.Internal,
            "internal.unexpected_error",
            "An unexpected error occurred.");

        return ToProblemResult(error);
    }

    private static IResult ToProblemResult(AppError error)
    {
        var statusCode = ToStatusCode(error.Type);
        var extensions = new Dictionary<string, object?>
        {
            ["code"] = error.Code,
            ["errorType"] = error.Type.ToString()
        };

        return HttpResults.Problem(
            detail: error.Message,
            statusCode: statusCode,
            title: ToTitle(error.Type),
            type: $"https://ticketgate.local/problems/{error.Code}",
            extensions: extensions);
    }

    private static int ToStatusCode(AppErrorType type)
    {
        return type switch
        {
            AppErrorType.NotFound => StatusCodes.Status404NotFound,
            AppErrorType.Conflict => StatusCodes.Status409Conflict,
            AppErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            AppErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string ToTitle(AppErrorType type)
    {
        return type switch
        {
            AppErrorType.NotFound => "Resource not found",
            AppErrorType.Conflict => "Conflict",
            AppErrorType.Validation => "Validation failed",
            AppErrorType.Unauthorized => "Unauthorized",
            _ => "Internal server error"
        };
    }
}

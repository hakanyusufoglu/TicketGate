namespace TicketGate.Core.Errors;

public sealed record AppError(AppErrorType Type, string Code, string Message)
{
    public static AppError NotFound(string entity, object id)
    {
        var normalizedEntity = string.IsNullOrWhiteSpace(entity) ? "resource" : entity.Trim();
        var normalizedCode = normalizedEntity.Replace(' ', '.').ToLowerInvariant();

        return new AppError(
            AppErrorType.NotFound,
            $"{normalizedCode}.not_found",
            $"{normalizedEntity} with id '{id}' was not found.");
    }

    public static AppError Conflict(string code, string message)
    {
        return new AppError(AppErrorType.Conflict, code, message);
    }

    public static AppError Validation(string code, string message)
    {
        return new AppError(AppErrorType.Validation, code, message);
    }

    public static AppError Unauthorized(string message)
    {
        return new AppError(AppErrorType.Unauthorized, "auth.unauthorized", message);
    }

    public static AppError TicketAlreadyLocked(Guid ticketId)
    {
        return new AppError(
            AppErrorType.Conflict,
            "ticket.already_locked",
            $"Ticket '{ticketId}' is already locked.");
    }

    public static AppError ConcurrencyConflict()
    {
        return new AppError(
            AppErrorType.Conflict,
            "concurrency.conflict",
            "The resource was modified by another operation. Reload and retry.");
    }
}

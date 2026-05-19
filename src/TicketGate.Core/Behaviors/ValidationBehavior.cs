using FluentValidation;
using Mediator;
using TicketGate.Core.Errors;
using TicketGate.Core.Results;

namespace TicketGate.Core.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var validatorList = validators as IReadOnlyCollection<IValidator<TRequest>> ?? validators.ToArray();

        if (validatorList.Count == 0)
        {
            return await next(request, cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationTasks = validatorList.Select(validator => validator.ValidateAsync(context, cancellationToken));
        var validationResults = await Task.WhenAll(validationTasks);

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => $"{failure.PropertyName}: {failure.ErrorMessage}")
            .Distinct()
            .ToArray();

        if (failures.Length == 0)
        {
            return await next(request, cancellationToken);
        }

        var error = AppError.Validation("validation.failed", string.Join("; ", failures));

        return CreateFailureResponse(error);
    }

    private static TResponse CreateFailureResponse(AppError error)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Fail(error);
        }

        if (!typeof(TResponse).IsGenericType ||
            typeof(TResponse).GetGenericTypeDefinition() != typeof(Result<>))
        {
            return default!;
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var resultType = typeof(Result<>).MakeGenericType(valueType);
        var failMethod = resultType.GetMethod(nameof(Result<object>.Fail), [typeof(AppError)]);
        var failedResult = failMethod?.Invoke(null, [error]);

        return failedResult is TResponse response ? response : default!;
    }
}

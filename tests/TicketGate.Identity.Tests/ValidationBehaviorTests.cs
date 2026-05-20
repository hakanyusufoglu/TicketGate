using FluentValidation;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketGate.Core.Behaviors;
using TicketGate.Core.Errors;
using TicketGate.Core.Extensions;
using TicketGate.Core.Results;

namespace TicketGate.Identity.Tests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public void AddModules_ShouldRegisterValidationBehaviorAsScoped()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Identity"] = "Host=localhost;Database=ticketgate_test;Username=test;Password=test",
                ["JwtSettings:SecretKey"] = "test-secret-key-that-is-long-enough-for-hmac-sha",
                ["JwtSettings:Issuer"] = "TicketGate.Tests",
                ["JwtSettings:Audience"] = "TicketGate.Tests"
            })
            .Build();

        services.AddModules(config);

        var descriptor = services.Last(service =>
            service.ServiceType.IsGenericType &&
            service.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(ValidationBehavior<,>), descriptor.ImplementationType);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenValidatorFails()
    {
        var validators = new IValidator<CreateIdentityCommand>[]
        {
            new CreateIdentityCommandValidator()
        };
        var behavior = new ValidationBehavior<CreateIdentityCommand, Result<string>>(validators);

        var result = await behavior.Handle(
            new CreateIdentityCommand(""),
            (_, _) => ValueTask.FromResult(Result<string>.Ok("created")),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AppErrorType.Validation, result.Error?.Type);
        Assert.Equal("validation.failed", result.Error?.Code);
        Assert.Contains("Name", result.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_ShouldCallNext_WhenNoValidatorsExist()
    {
        var behavior = new ValidationBehavior<CreateIdentityCommand, Result<string>>([]);
        var nextCalled = false;

        var result = await behavior.Handle(
            new CreateIdentityCommand("identity"),
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.FromResult(Result<string>.Ok("created"));
            },
            CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
        Assert.Equal("created", result.Value);
    }

    public sealed record CreateIdentityCommand(string Name) : IRequest<Result<string>>;

    private sealed class CreateIdentityCommandValidator : AbstractValidator<CreateIdentityCommand>
    {
        public CreateIdentityCommandValidator()
        {
            RuleFor(command => command.Name).NotEmpty();
        }
    }
}

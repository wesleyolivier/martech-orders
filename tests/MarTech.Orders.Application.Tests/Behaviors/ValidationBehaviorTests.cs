using FluentValidation;
using MarTech.Orders.Application.Behaviors;
using MarTech.Orders.Application.Orders.CreateOrder;
using MediatR;

namespace MarTech.Orders.Application.Tests.Behaviors;

public sealed class ValidationBehaviorTests
{
    private static readonly Guid CustomerId = Guid.Parse("2f0a5c3d-9b7e-4a11-8c62-5d4e3f2a1b09");

    [Fact]
    public async Task Handle_WhenRequestIsValid_CallsTheNextStep()
    {
        var behavior = Behavior([new CreateOrderCommandValidator()]);
        var command = new CreateOrderCommand(CustomerId, [new CreateOrderItem("Keyboard", 1, 10m)]);
        var called = false;

        await behavior.Handle(command, Next(() => called = true), CancellationToken.None);

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenRequestIsInvalid_ThrowsBeforeTheNextStep()
    {
        var behavior = Behavior([new CreateOrderCommandValidator()]);
        var command = new CreateOrderCommand(CustomerId, []);
        var called = false;

        var exception = await Should.ThrowAsync<ValidationException>(
            () => behavior.Handle(command, Next(() => called = true), CancellationToken.None));

        called.ShouldBeFalse();
        exception.Errors.ShouldContain(failure => failure.PropertyName == nameof(CreateOrderCommand.Items));
    }

    [Fact]
    public async Task Handle_WithoutValidators_CallsTheNextStep()
    {
        var behavior = Behavior([]);
        var command = new CreateOrderCommand(Guid.Empty, []);
        var called = false;

        await behavior.Handle(command, Next(() => called = true), CancellationToken.None);

        called.ShouldBeTrue();
    }

    private static ValidationBehavior<CreateOrderCommand, Unit> Behavior(
        IEnumerable<IValidator<CreateOrderCommand>> validators) => new(validators);

    private static RequestHandlerDelegate<Unit> Next(Action onCalled) => _ =>
    {
        onCalled();
        return Task.FromResult(Unit.Value);
    };
}

using FluentValidation;

namespace MarTech.Orders.Application.Orders.CancelOrder;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator() => RuleFor(command => command.OrderId).NotEmpty();
}

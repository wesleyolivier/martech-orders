using FluentValidation;
using MarTech.Orders.Domain.Orders;

namespace MarTech.Orders.Application.Orders.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty();

        RuleFor(command => command.Items)
            .NotNull()
            .NotEmpty()
            .WithMessage("An order must contain at least one item.");

        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(orderItem => orderItem.ProductName)
                .NotEmpty()
                .MaximumLength(200);

            item.RuleFor(orderItem => orderItem.Quantity)
                .GreaterThan(0);

            item.RuleFor(orderItem => orderItem.UnitPrice)
                .GreaterThan(0)
                .Must(unitPrice => decimal.Round(unitPrice, Money.Scale) == unitPrice)
                .WithMessage($"'{{PropertyName}}' must have at most {Money.Scale} decimal places.");
        });
    }
}

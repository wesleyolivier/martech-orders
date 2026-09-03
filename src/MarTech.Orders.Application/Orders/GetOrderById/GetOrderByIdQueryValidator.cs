using FluentValidation;

namespace MarTech.Orders.Application.Orders.GetOrderById;

public sealed class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator() => RuleFor(query => query.OrderId).NotEmpty();
}

using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Common.Exceptions;
using MarTech.Orders.Application.Orders.Contracts;
using MediatR;

namespace MarTech.Orders.Application.Orders.GetOrderById;

public sealed class GetOrderByIdQueryHandler(IOrderReadRepository orders)
    : IRequestHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<OrderResponse> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken) =>
        await orders.GetByIdAsync(request.OrderId, cancellationToken)
        ?? throw new OrderNotFoundException(request.OrderId);
}

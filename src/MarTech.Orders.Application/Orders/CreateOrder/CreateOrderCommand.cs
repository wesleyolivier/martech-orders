using MarTech.Orders.Application.Orders.Contracts;
using MediatR;

namespace MarTech.Orders.Application.Orders.CreateOrder;

public sealed record CreateOrderCommand(Guid CustomerId, IReadOnlyList<CreateOrderItem> Items)
    : IRequest<OrderResponse>;

public sealed record CreateOrderItem(string ProductName, int Quantity, decimal UnitPrice);

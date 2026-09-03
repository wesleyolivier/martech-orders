using MarTech.Orders.Application.Orders.Contracts;
using MediatR;

namespace MarTech.Orders.Application.Orders.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderResponse>;

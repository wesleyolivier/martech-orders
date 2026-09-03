using MediatR;

namespace MarTech.Orders.Application.Orders.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest;

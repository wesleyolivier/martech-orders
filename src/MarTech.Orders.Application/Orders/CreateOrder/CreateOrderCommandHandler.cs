using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Orders.Contracts;
using MarTech.Orders.Application.Orders.Mapping;
using MarTech.Orders.Domain.Orders;
using MediatR;

namespace MarTech.Orders.Application.Orders.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var drafts = request.Items
            .Select(item => new OrderItemDraft(item.ProductName, item.Quantity, item.UnitPrice))
            .ToArray();

        var order = Order.Place(request.CustomerId, drafts, dateTimeProvider.UtcNow);

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToResponse();
    }
}

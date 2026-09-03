using MarTech.Orders.Application.Orders.Contracts;
using MarTech.Orders.Domain.Orders;

namespace MarTech.Orders.Application.Orders.Mapping;

public static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order) => new(
        order.Id,
        order.CustomerId,
        order.Status.ToString(),
        order.CreatedAt,
        order.TotalAmount,
        [.. order.Items.Select(item => new OrderItemResponse(
            item.Id,
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal))]);
}

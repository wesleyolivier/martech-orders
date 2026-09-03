namespace MarTech.Orders.Application.Orders.Contracts;

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTime CreatedAt,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponse> Items);

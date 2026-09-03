namespace MarTech.Orders.Application.Orders.Contracts;

public sealed record OrderSummaryResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTime CreatedAt,
    decimal TotalAmount,
    int ItemCount);

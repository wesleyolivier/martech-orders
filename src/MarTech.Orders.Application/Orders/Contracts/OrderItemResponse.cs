namespace MarTech.Orders.Application.Orders.Contracts;

public sealed record OrderItemResponse(Guid Id, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

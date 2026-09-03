namespace MarTech.Orders.Domain.Orders;

public readonly record struct OrderItemDraft(string ProductName, int Quantity, decimal UnitPrice);

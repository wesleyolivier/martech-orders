namespace MarTech.Orders.Api.Contracts;

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(string ProductName, int Quantity, decimal UnitPrice);

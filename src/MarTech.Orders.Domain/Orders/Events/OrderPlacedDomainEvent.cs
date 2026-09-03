using MarTech.Orders.Domain.Common;

namespace MarTech.Orders.Domain.Orders.Events;

public sealed record OrderPlacedDomainEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount, DateTime OccurredAt)
    : IDomainEvent;

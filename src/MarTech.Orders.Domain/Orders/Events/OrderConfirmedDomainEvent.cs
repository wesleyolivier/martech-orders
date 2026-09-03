using MarTech.Orders.Domain.Common;

namespace MarTech.Orders.Domain.Orders.Events;

public sealed record OrderConfirmedDomainEvent(Guid OrderId, Guid CustomerId, DateTime OccurredAt) : IDomainEvent;

using MarTech.Orders.Domain.Common;
using MediatR;

namespace MarTech.Orders.Infrastructure.Persistence.DomainEvents;

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;

namespace MarTech.Orders.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Domain.Common;
using MarTech.Orders.Infrastructure.Persistence.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace MarTech.Orders.Infrastructure.Persistence;

public sealed class UnitOfWork(OrdersDbContext context, DomainEventDispatcher dispatcher) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var entities = context.ChangeTracker
            .Entries<Entity>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToArray();

        var domainEvents = entities.SelectMany(entity => entity.DomainEvents).ToArray();

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        var affected = await context.SaveChangesAsync(cancellationToken);

        await dispatcher.DispatchAsync(domainEvents, cancellationToken);

        return affected;
    }
}

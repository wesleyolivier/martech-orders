using MarTech.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace MarTech.Orders.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(OrdersDbContext context) : IOrderRepository
{
    public void Add(Order order) => context.Orders.Add(order);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
}

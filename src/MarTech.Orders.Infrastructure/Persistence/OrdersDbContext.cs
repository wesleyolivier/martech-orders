using MarTech.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace MarTech.Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}

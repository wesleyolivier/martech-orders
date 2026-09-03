namespace MarTech.Orders.Domain.Orders;

public interface IOrderRepository
{
    void Add(Order order);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

using MarTech.Orders.Domain.Common;
using MarTech.Orders.Domain.Exceptions;
using MarTech.Orders.Domain.Orders.Events;

namespace MarTech.Orders.Domain.Orders;

public sealed class Order : Entity
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    private Order(Guid customerId, DateTime createdAt)
    {
        Id = Guid.CreateVersion7();
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
    }

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public decimal TotalAmount { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public static Order Place(Guid customerId, IEnumerable<OrderItemDraft> items, DateTime createdAt)
    {
        if (customerId == Guid.Empty)
        {
            throw new InvalidCustomerException();
        }

        var order = new Order(customerId, createdAt);

        foreach (var draft in items)
        {
            order._items.Add(OrderItem.Create(order.Id, draft.ProductName, draft.Quantity, draft.UnitPrice));
        }

        if (order._items.Count == 0)
        {
            throw new EmptyOrderException();
        }

        order.RecalculateTotal();
        order.Raise(new OrderPlacedDomainEvent(order.Id, order.CustomerId, order.TotalAmount, createdAt));

        return order;
    }

    public void Cancel(DateTime cancelledAt)
    {
        if (Status is not OrderStatus.Pending)
        {
            throw new OrderNotCancellableException(Id, Status);
        }

        Status = OrderStatus.Cancelled;
        Raise(new OrderCancelledDomainEvent(Id, CustomerId, cancelledAt));
    }

    public void Confirm(DateTime confirmedAt)
    {
        if (Status is not OrderStatus.Pending)
        {
            throw new OrderNotConfirmableException(Id, Status);
        }

        Status = OrderStatus.Confirmed;
        Raise(new OrderConfirmedDomainEvent(Id, CustomerId, confirmedAt));
    }

    private void RecalculateTotal() => TotalAmount = Money.Normalize(_items.Sum(item => item.LineTotal));
}

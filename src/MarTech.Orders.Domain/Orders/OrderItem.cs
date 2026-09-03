using MarTech.Orders.Domain.Common;
using MarTech.Orders.Domain.Exceptions;

namespace MarTech.Orders.Domain.Orders;

public sealed class OrderItem : Entity
{
    private OrderItem()
    {
        ProductName = string.Empty;
    }

    private OrderItem(Guid orderId, string productName, int quantity, decimal unitPrice)
    {
        Id = Guid.CreateVersion7();
        OrderId = orderId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid OrderId { get; private set; }

    public string ProductName { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal static OrderItem Create(Guid orderId, string productName, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new InvalidProductNameException();
        }

        if (quantity <= 0)
        {
            throw new InvalidQuantityException(quantity);
        }

        if (unitPrice <= 0)
        {
            throw new InvalidUnitPriceException(unitPrice);
        }

        return new OrderItem(orderId, productName.Trim(), quantity, Money.Normalize(unitPrice));
    }
}

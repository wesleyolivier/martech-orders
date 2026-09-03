using MarTech.Orders.Domain.Orders;

namespace MarTech.Orders.Domain.Exceptions;

public sealed class EmptyOrderException()
    : DomainException("An order must contain at least one item.");

public sealed class InvalidCustomerException()
    : DomainException("An order must be placed for a valid customer.");

public sealed class InvalidProductNameException()
    : DomainException("Product name is required.");

public sealed class InvalidQuantityException(int quantity)
    : DomainException($"Quantity must be greater than zero, but was {quantity}.");

public sealed class InvalidUnitPriceException(decimal unitPrice)
    : DomainException($"Unit price must be greater than zero, but was {unitPrice}.");

public sealed class UnsupportedMonetaryPrecisionException(decimal value)
    : DomainException($"Monetary values support at most two decimal places, but {value} was given.");

public sealed class OrderNotCancellableException(Guid orderId, OrderStatus status)
    : DomainException($"Order {orderId} cannot be cancelled because its status is {status}.")
{
    public Guid OrderId { get; } = orderId;

    public OrderStatus Status { get; } = status;
}

public sealed class OrderNotConfirmableException(Guid orderId, OrderStatus status)
    : DomainException($"Order {orderId} cannot be confirmed because its status is {status}.")
{
    public Guid OrderId { get; } = orderId;

    public OrderStatus Status { get; } = status;
}

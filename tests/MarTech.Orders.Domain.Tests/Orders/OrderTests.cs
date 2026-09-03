using MarTech.Orders.Domain.Exceptions;
using MarTech.Orders.Domain.Orders;
using MarTech.Orders.Domain.Orders.Events;

namespace MarTech.Orders.Domain.Tests.Orders;

public sealed class OrderTests
{
    private static readonly Guid CustomerId = Guid.Parse("2f0a5c3d-9b7e-4a11-8c62-5d4e3f2a1b09");
    private static readonly DateTime CreatedAt = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Place_WithValidItems_CreatesPendingOrder()
    {
        var order = Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 2, 149.90m)], CreatedAt);

        order.Id.ShouldNotBe(Guid.Empty);
        order.CustomerId.ShouldBe(CustomerId);
        order.Status.ShouldBe(OrderStatus.Pending);
        order.CreatedAt.ShouldBe(CreatedAt);
        order.Items.Count.ShouldBe(1);
    }

    [Fact]
    public void Place_SumsLineTotalsIntoTotalAmount()
    {
        var order = Order.Place(
            CustomerId,
            [new OrderItemDraft("Keyboard", 2, 149.90m), new OrderItemDraft("Mouse", 1, 89.50m)],
            CreatedAt);

        order.TotalAmount.ShouldBe(389.30m);
    }

    [Fact]
    public void Place_AssignsOrderIdToEveryItem()
    {
        var order = Order.Place(
            CustomerId,
            [new OrderItemDraft("Keyboard", 1, 10m), new OrderItemDraft("Mouse", 1, 20m)],
            CreatedAt);

        order.Items.ShouldAllBe(item => item.OrderId == order.Id);
    }

    [Fact]
    public void Place_RaisesOrderPlacedDomainEvent()
    {
        var order = Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 1, 100m)], CreatedAt);

        var domainEvent = order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderPlacedDomainEvent>();
        domainEvent.OrderId.ShouldBe(order.Id);
        domainEvent.CustomerId.ShouldBe(CustomerId);
        domainEvent.TotalAmount.ShouldBe(100m);
    }

    [Fact]
    public void Place_WithoutItems_Throws()
    {
        var act = () => Order.Place(CustomerId, [], CreatedAt);

        act.ShouldThrow<EmptyOrderException>();
    }

    [Fact]
    public void Place_WithEmptyCustomer_Throws()
    {
        var act = () => Order.Place(Guid.Empty, [new OrderItemDraft("Keyboard", 1, 10m)], CreatedAt);

        act.ShouldThrow<InvalidCustomerException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Place_WithNonPositiveQuantity_Throws(int quantity)
    {
        var act = () => Order.Place(CustomerId, [new OrderItemDraft("Keyboard", quantity, 10m)], CreatedAt);

        act.ShouldThrow<InvalidQuantityException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Place_WithNonPositiveUnitPrice_Throws(decimal unitPrice)
    {
        var act = () => Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 1, unitPrice)], CreatedAt);

        act.ShouldThrow<InvalidUnitPriceException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Place_WithBlankProductName_Throws(string productName)
    {
        var act = () => Order.Place(CustomerId, [new OrderItemDraft(productName, 1, 10m)], CreatedAt);

        act.ShouldThrow<InvalidProductNameException>();
    }

    [Fact]
    public void Place_WithMoreThanTwoDecimalPlaces_Throws()
    {
        var act = () => Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 1, 10.123m)], CreatedAt);

        act.ShouldThrow<UnsupportedMonetaryPrecisionException>();
    }

    [Fact]
    public void Cancel_WhenPending_MovesToCancelled()
    {
        var order = Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 1, 10m)], CreatedAt);
        order.ClearDomainEvents();

        order.Cancel(CreatedAt.AddMinutes(5));

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderCancelledDomainEvent>();
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var order = Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 1, 10m)], CreatedAt);
        order.Cancel(CreatedAt);

        var act = () => order.Cancel(CreatedAt);

        act.ShouldThrow<OrderNotCancellableException>().Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenConfirmed_Throws()
    {
        var order = Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 1, 10m)], CreatedAt);
        order.Confirm(CreatedAt);

        var act = () => order.Cancel(CreatedAt);

        act.ShouldThrow<OrderNotCancellableException>().Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WhenCancelled_Throws()
    {
        var order = Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 1, 10m)], CreatedAt);
        order.Cancel(CreatedAt);

        var act = () => order.Confirm(CreatedAt);

        act.ShouldThrow<OrderNotConfirmableException>();
    }

    [Fact]
    public void Items_IsNotDirectlyMutable()
    {
        var order = Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 1, 10m)], CreatedAt);

        (order.Items is ICollection<OrderItem> { IsReadOnly: false }).ShouldBeFalse();
    }
}

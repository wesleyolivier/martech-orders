using MarTech.Orders.Domain.Orders;

namespace MarTech.Orders.Domain.Tests.Orders;

public sealed class OrderItemTests
{
    private static readonly Guid CustomerId = Guid.Parse("2f0a5c3d-9b7e-4a11-8c62-5d4e3f2a1b09");
    private static readonly DateTime CreatedAt = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(1, 10.00, 10.00)]
    [InlineData(3, 19.99, 59.97)]
    [InlineData(10, 0.01, 0.10)]
    public void LineTotal_MultipliesQuantityByUnitPrice(int quantity, decimal unitPrice, decimal expected)
    {
        var order = Order.Place(CustomerId, [new OrderItemDraft("Keyboard", quantity, unitPrice)], CreatedAt);

        order.Items[0].LineTotal.ShouldBe(expected);
    }

    [Fact]
    public void Create_TrimsProductName()
    {
        var order = Order.Place(CustomerId, [new OrderItemDraft("  Keyboard  ", 1, 10m)], CreatedAt);

        order.Items[0].ProductName.ShouldBe("Keyboard");
    }
}

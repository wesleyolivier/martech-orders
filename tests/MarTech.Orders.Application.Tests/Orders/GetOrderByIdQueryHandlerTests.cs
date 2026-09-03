using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Common.Exceptions;
using MarTech.Orders.Application.Orders.Contracts;
using MarTech.Orders.Application.Orders.GetOrderById;
using NSubstitute;

namespace MarTech.Orders.Application.Tests.Orders;

public sealed class GetOrderByIdQueryHandlerTests
{
    private readonly IOrderReadRepository _orders = Substitute.For<IOrderReadRepository>();
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests() => _handler = new GetOrderByIdQueryHandler(_orders);

    [Fact]
    public async Task Handle_WhenOrderExists_ReturnsIt()
    {
        var orderId = Guid.CreateVersion7();
        var expected = new OrderResponse(
            orderId,
            Guid.CreateVersion7(),
            "Pending",
            new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc),
            10m,
            []);

        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(expected);

        var response = await _handler.Handle(new GetOrderByIdQuery(orderId), CancellationToken.None);

        response.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenOrderIsMissing_Throws()
    {
        var orderId = Guid.CreateVersion7();
        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((OrderResponse?)null);

        var exception = await Should.ThrowAsync<OrderNotFoundException>(
            () => _handler.Handle(new GetOrderByIdQuery(orderId), CancellationToken.None));

        exception.OrderId.ShouldBe(orderId);
    }
}

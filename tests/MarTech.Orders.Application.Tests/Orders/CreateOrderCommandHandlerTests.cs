using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Orders.CreateOrder;
using MarTech.Orders.Application.Tests.Common;
using MarTech.Orders.Domain.Exceptions;
using MarTech.Orders.Domain.Orders;
using NSubstitute;

namespace MarTech.Orders.Application.Tests.Orders;

public sealed class CreateOrderCommandHandlerTests
{
    private static readonly Guid CustomerId = Guid.Parse("2f0a5c3d-9b7e-4a11-8c62-5d4e3f2a1b09");
    private static readonly DateTime Now = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests() =>
        _handler = new CreateOrderCommandHandler(_orders, _unitOfWork, new FixedDateTimeProvider(Now));

    [Fact]
    public async Task Handle_PersistsOrderAndCommitsOnce()
    {
        var command = new CreateOrderCommand(CustomerId, [new CreateOrderItem("Keyboard", 2, 149.90m)]);

        await _handler.Handle(command, CancellationToken.None);

        _orders.Received(1).Add(Arg.Is<Order>(order => order.CustomerId == CustomerId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsResponseWithTotalCalculatedByTheDomain()
    {
        var command = new CreateOrderCommand(
            CustomerId,
            [new CreateOrderItem("Keyboard", 2, 149.90m), new CreateOrderItem("Mouse", 1, 89.50m)]);

        var response = await _handler.Handle(command, CancellationToken.None);

        response.TotalAmount.ShouldBe(389.30m);
        response.Items.Count.ShouldBe(2);
        response.Items.Sum(item => item.LineTotal).ShouldBe(response.TotalAmount);
    }

    [Fact]
    public async Task Handle_StampsCreatedAtFromTheTimeProvider()
    {
        var command = new CreateOrderCommand(CustomerId, [new CreateOrderItem("Keyboard", 1, 10m)]);

        var response = await _handler.Handle(command, CancellationToken.None);

        response.CreatedAt.ShouldBe(Now);
        response.Status.ShouldBe(nameof(OrderStatus.Pending));
    }

    [Fact]
    public async Task Handle_WithoutItems_DoesNotCommit()
    {
        var command = new CreateOrderCommand(CustomerId, []);

        await Should.ThrowAsync<EmptyOrderException>(() => _handler.Handle(command, CancellationToken.None));

        _orders.DidNotReceive().Add(Arg.Any<Order>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

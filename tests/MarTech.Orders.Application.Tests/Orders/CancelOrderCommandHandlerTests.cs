using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Common.Exceptions;
using MarTech.Orders.Application.Orders.CancelOrder;
using MarTech.Orders.Application.Tests.Common;
using MarTech.Orders.Domain.Exceptions;
using MarTech.Orders.Domain.Orders;
using NSubstitute;

namespace MarTech.Orders.Application.Tests.Orders;

public sealed class CancelOrderCommandHandlerTests
{
    private static readonly Guid CustomerId = Guid.Parse("2f0a5c3d-9b7e-4a11-8c62-5d4e3f2a1b09");
    private static readonly DateTime Now = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests() =>
        _handler = new CancelOrderCommandHandler(_orders, _unitOfWork, new FixedDateTimeProvider(Now));

    [Fact]
    public async Task Handle_WhenOrderIsPending_CancelsAndCommits()
    {
        var order = PendingOrder();
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        order.Status.ShouldBe(OrderStatus.Cancelled);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_ThrowsAndDoesNotCommit()
    {
        var orderId = Guid.CreateVersion7();
        _orders.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var exception = await Should.ThrowAsync<OrderNotFoundException>(
            () => _handler.Handle(new CancelOrderCommand(orderId), CancellationToken.None));

        exception.OrderId.ShouldBe(orderId);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOrderIsNotPending_ThrowsAndDoesNotCommit()
    {
        var order = PendingOrder();
        order.Confirm(Now);
        _orders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await Should.ThrowAsync<OrderNotCancellableException>(
            () => _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None));

        order.Status.ShouldBe(OrderStatus.Confirmed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Order PendingOrder() =>
        Order.Place(CustomerId, [new OrderItemDraft("Keyboard", 1, 10m)], Now);
}

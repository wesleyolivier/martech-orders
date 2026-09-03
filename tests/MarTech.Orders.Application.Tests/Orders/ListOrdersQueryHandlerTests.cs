using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Common;
using MarTech.Orders.Application.Orders.Contracts;
using MarTech.Orders.Application.Orders.ListOrders;
using NSubstitute;

namespace MarTech.Orders.Application.Tests.Orders;

public sealed class ListOrdersQueryHandlerTests
{
    private readonly IOrderReadRepository _orders = Substitute.For<IOrderReadRepository>();
    private readonly ListOrdersQueryHandler _handler;

    public ListOrdersQueryHandlerTests() => _handler = new ListOrdersQueryHandler(_orders);

    [Fact]
    public async Task Handle_ForwardsPagingArgumentsToTheReadModel()
    {
        _orders.ListAsync(2, 25, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<OrderSummaryResponse>([], 2, 25, 0));

        await _handler.Handle(new ListOrdersQuery(2, 25), CancellationToken.None);

        await _orders.Received(1).ListAsync(2, 25, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsesTenItemsPerPageByDefault()
    {
        var query = new ListOrdersQuery();

        _orders.ListAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<OrderSummaryResponse>([], 1, 10, 0));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
        await _orders.Received(1).ListAsync(1, 10, Arg.Any<CancellationToken>());
    }
}

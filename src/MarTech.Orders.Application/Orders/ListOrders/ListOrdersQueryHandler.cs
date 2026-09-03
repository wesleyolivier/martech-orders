using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Common;
using MarTech.Orders.Application.Orders.Contracts;
using MediatR;

namespace MarTech.Orders.Application.Orders.ListOrders;

public sealed class ListOrdersQueryHandler(IOrderReadRepository orders)
    : IRequestHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    public Task<PagedResult<OrderSummaryResponse>> Handle(
        ListOrdersQuery request,
        CancellationToken cancellationToken) =>
        orders.ListAsync(request.Page, request.PageSize, cancellationToken);
}

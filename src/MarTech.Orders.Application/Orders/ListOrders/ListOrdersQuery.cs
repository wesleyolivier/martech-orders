using MarTech.Orders.Application.Common;
using MarTech.Orders.Application.Orders.Contracts;
using MediatR;

namespace MarTech.Orders.Application.Orders.ListOrders;

public sealed record ListOrdersQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<OrderSummaryResponse>>
{
    public const int MaxPageSize = 100;
}

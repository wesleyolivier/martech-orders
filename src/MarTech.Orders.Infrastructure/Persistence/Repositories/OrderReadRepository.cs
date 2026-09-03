using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Common;
using MarTech.Orders.Application.Orders.Contracts;
using MarTech.Orders.Application.Orders.Mapping;
using Microsoft.EntityFrameworkCore;

namespace MarTech.Orders.Infrastructure.Persistence.Repositories;

public sealed class OrderReadRepository(OrdersDbContext context) : IOrderReadRepository
{
    public async Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

        return order?.ToResponse();
    }

    public async Task<PagedResult<OrderSummaryResponse>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.Orders.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return new PagedResult<OrderSummaryResponse>([], page, pageSize, 0);
        }

        var rows = await query
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new
            {
                order.Id,
                order.CustomerId,
                order.Status,
                order.CreatedAt,
                order.TotalAmount,
                ItemCount = order.Items.Count
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new OrderSummaryResponse(
                row.Id,
                row.CustomerId,
                row.Status.ToString(),
                row.CreatedAt,
                row.TotalAmount,
                row.ItemCount))
            .ToList();

        return new PagedResult<OrderSummaryResponse>(items, page, pageSize, totalCount);
    }
}

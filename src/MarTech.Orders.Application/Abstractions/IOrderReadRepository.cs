using MarTech.Orders.Application.Common;
using MarTech.Orders.Application.Orders.Contracts;

namespace MarTech.Orders.Application.Abstractions;

public interface IOrderReadRepository
{
    Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<OrderSummaryResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}

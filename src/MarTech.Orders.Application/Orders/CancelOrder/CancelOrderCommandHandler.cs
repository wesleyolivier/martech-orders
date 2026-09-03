using MarTech.Orders.Application.Abstractions;
using MarTech.Orders.Application.Common.Exceptions;
using MarTech.Orders.Domain.Orders;
using MediatR;

namespace MarTech.Orders.Application.Orders.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
                    ?? throw new OrderNotFoundException(request.OrderId);

        order.Cancel(dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using MarTech.Orders.Application.Abstractions;

namespace MarTech.Orders.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

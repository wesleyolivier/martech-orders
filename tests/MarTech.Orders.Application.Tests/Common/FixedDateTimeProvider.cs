using MarTech.Orders.Application.Abstractions;

namespace MarTech.Orders.Application.Tests.Common;

public sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = utcNow;
}

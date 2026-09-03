namespace MarTech.Orders.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

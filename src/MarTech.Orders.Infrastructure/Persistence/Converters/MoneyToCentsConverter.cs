using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MarTech.Orders.Infrastructure.Persistence.Converters;

public sealed class MoneyToCentsConverter() : ValueConverter<decimal, long>(
    amount => decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero)),
    cents => cents / 100m);

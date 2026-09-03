using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MarTech.Orders.Infrastructure.Persistence.Converters;

public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

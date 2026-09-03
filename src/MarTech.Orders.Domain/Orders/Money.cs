using MarTech.Orders.Domain.Exceptions;

namespace MarTech.Orders.Domain.Orders;

public static class Money
{
    public const int Scale = 2;

    public static decimal Normalize(decimal value)
    {
        if (decimal.Round(value, Scale) != value)
        {
            throw new UnsupportedMonetaryPrecisionException(value);
        }

        return decimal.Round(value, Scale);
    }
}

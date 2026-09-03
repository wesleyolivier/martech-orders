using MarTech.Orders.Domain.Orders;
using MarTech.Orders.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarTech.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.Quantity).IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasConversion(new MoneyToCentsConverter())
            .IsRequired();

        builder.Ignore(item => item.LineTotal);

        builder.Ignore(item => item.DomainEvents);

        builder.HasIndex(item => item.OrderId);
    }
}

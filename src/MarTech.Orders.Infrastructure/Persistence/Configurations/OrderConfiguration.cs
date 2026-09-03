using MarTech.Orders.Domain.Orders;
using MarTech.Orders.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarTech.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.CustomerId).IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(order => order.CreatedAt)
            .HasConversion(new UtcDateTimeConverter())
            .IsRequired();

        builder.Property(order => order.TotalAmount)
            .HasConversion(new MoneyToCentsConverter())
            .IsRequired();

        builder.Ignore(order => order.DomainEvents);

        builder.Metadata
            .FindNavigation(nameof(Order.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(order => order.CreatedAt);

        builder.HasIndex(order => order.CustomerId);
    }
}

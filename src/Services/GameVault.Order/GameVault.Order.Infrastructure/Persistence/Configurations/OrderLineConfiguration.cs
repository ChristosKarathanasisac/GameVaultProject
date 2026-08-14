using GameVault.Order.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameVault.Order.Infrastructure.Persistence.Configurations;

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .ValueGeneratedNever();

        builder.Property(l => l.OrderId)
            .IsRequired();

        // No FK constraint to Catalog's Products table — cross-service reference
        builder.Property(l => l.ProductId)
            .IsRequired();

        builder.Property(l => l.ProductTitle)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(l => l.Quantity)
            .IsRequired();

        builder.HasIndex(l => l.OrderId)
            .HasDatabaseName("IX_OrderLines_OrderId");
    }
}

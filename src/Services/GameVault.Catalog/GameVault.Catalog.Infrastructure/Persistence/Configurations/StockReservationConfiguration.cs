using GameVault.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameVault.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.ProductId)
            .IsRequired();

        builder.Property(r => r.OrderId)
            .IsRequired();

        builder.Property(r => r.Quantity)
            .IsRequired();

        // Stored as smallint (PostgreSQL has no tinyint, byte maps to smallint via Npgsql)
        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired();

        // Idempotency key — a retry for the same order+product hits this and is treated as no-op
        builder.HasIndex(r => new { r.OrderId, r.ProductId })
            .IsUnique()
            .HasDatabaseName("UX_StockReservations_OrderId_ProductId");

        builder.HasIndex(r => new { r.Status, r.ExpiresAt })
            .HasDatabaseName("IX_StockReservations_Status_ExpiresAt");
    }
}

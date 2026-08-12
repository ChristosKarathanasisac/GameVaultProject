using GameVault.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameVault.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stock");

        builder.HasKey(s => s.ProductId);

        builder.Property(s => s.ProductId)
            .ValueGeneratedNever();

        builder.Property(s => s.AvailableStock)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}

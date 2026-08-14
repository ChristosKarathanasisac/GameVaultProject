using GameVault.Catalog.Domain.Exceptions;

namespace GameVault.Catalog.Domain.Entities;

public sealed class Stock
{
    private Stock() { }

    public Guid ProductId { get; private set; }
    public int AvailableStock { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Product? Product { get; private set; }

    public static Stock Create(Guid productId, int initialStock)
    {
        return new Stock
        {
            ProductId = productId,
            AvailableStock = initialStock,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Decrease(int quantity)
    {
        if (quantity > AvailableStock)
            throw new InsufficientStockException(ProductId, quantity, AvailableStock);

        AvailableStock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Increase(int quantity)
    {
        AvailableStock += quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}

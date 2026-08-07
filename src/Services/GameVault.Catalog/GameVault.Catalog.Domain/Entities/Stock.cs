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
}

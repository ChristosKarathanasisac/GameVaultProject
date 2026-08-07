namespace GameVault.Catalog.Domain.Entities;

public sealed class Product
{
    private Product() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Stock? Stock { get; private set; }
    public ICollection<StockReservation> Reservations { get; private set; } = [];

    public static Product Create(string name, string? description, decimal price)
    {
        return new Product
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = description,
            Price = price,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

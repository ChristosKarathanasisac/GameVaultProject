using GameVault.Catalog.Domain.Enums;

namespace GameVault.Catalog.Domain.Entities;

public sealed class StockReservation
{
    private StockReservation() { }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }

    public Guid OrderId { get; private set; }

    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Product? Product { get; private set; }

    public static StockReservation Reserve(Guid productId, Guid orderId, int quantity, DateTime expiresAt)
    {
        return new StockReservation
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            OrderId = orderId,
            Quantity = quantity,
            Status = ReservationStatus.Reserved,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Confirm()
    {
        Status = ReservationStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release()
    {
        Status = ReservationStatus.Released;
        UpdatedAt = DateTime.UtcNow;
    }
}

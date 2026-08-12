using GameVault.Catalog.Domain.Entities;

namespace GameVault.Catalog.Application.Abstractions;

public interface IStockReservationRepository
{
    Task<StockReservation?> GetByOrderAndProductAsync(Guid orderId, Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(StockReservation reservation, CancellationToken cancellationToken = default);
}

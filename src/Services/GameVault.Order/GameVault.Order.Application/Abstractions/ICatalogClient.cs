using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.Abstractions;

public interface ICatalogClient
{
    Task<Result<GameResponse>> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<Result<ReservationResponse>> ReserveStockAsync(Guid productId, Guid orderId, int quantity, CancellationToken cancellationToken = default);
    Task<Result<Unit>> ReleaseReservationAsync(Guid orderId, Guid productId, CancellationToken cancellationToken = default);
}

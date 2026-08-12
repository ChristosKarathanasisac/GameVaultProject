using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Reservations.ReserveStock;

public interface IReserveStockHandler
{
    Task<Result<ReservationResponse>> HandleAsync(ReserveStockCommand command, CancellationToken cancellationToken = default);
}

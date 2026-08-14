using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Reservations.ReleaseReservation;

public interface IReleaseReservationHandler
{
    Task<Result<ReservationResponse>> HandleAsync(ReleaseReservationCommand command, CancellationToken cancellationToken = default);
}

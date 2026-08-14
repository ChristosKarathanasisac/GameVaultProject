using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Reservations.ConfirmReservation;

public interface IConfirmReservationHandler
{
    Task<Result<ReservationResponse>> HandleAsync(ConfirmReservationCommand command, CancellationToken cancellationToken = default);
}

using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Application.Errors;
using GameVault.Catalog.Domain.Exceptions;
using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Reservations.ConfirmReservation;

public sealed class ConfirmReservationHandler : IConfirmReservationHandler
{
    private readonly IStockReservationRepository _reservationRepository;

    public ConfirmReservationHandler(IStockReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<ReservationResponse>> HandleAsync(ConfirmReservationCommand command, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByOrderAndProductAsync(command.OrderId, command.ProductId, cancellationToken);

        if (reservation is null)
            return CatalogErrors.ReservationNotFound;

        try
        {
            reservation.Confirm();
        }
        catch (InvalidReservationStatusException)
        {
            return CatalogErrors.InvalidReservationTransition;
        }

        await _reservationRepository.SaveChangesAsync(cancellationToken);

        return reservation.ToReservationResponse();
    }
}

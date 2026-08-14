using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Application.Errors;
using GameVault.Catalog.Domain.Exceptions;
using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Reservations.ReleaseReservation;

public sealed class ReleaseReservationHandler : IReleaseReservationHandler
{
    private readonly IProductRepository _productRepository;
    private readonly IStockReservationRepository _reservationRepository;

    public ReleaseReservationHandler(IProductRepository productRepository, IStockReservationRepository reservationRepository)
    {
        _productRepository = productRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<ReservationResponse>> HandleAsync(ReleaseReservationCommand command, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByOrderAndProductAsync(command.OrderId, command.ProductId, cancellationToken);

        if (reservation is null)
            return CatalogErrors.ReservationNotFound;

        try
        {
            reservation.Release();
        }
        catch (InvalidReservationStatusException)
        {
            return CatalogErrors.InvalidReservationTransition;
        }

        var product = await _productRepository.GetByIdAsync(reservation.ProductId, cancellationToken);

        if (product?.Stock is not null)
            product.Stock.Increase(reservation.Quantity);

        await _productRepository.SaveChangesAsync(cancellationToken);

        return reservation.ToReservationResponse();
    }
}

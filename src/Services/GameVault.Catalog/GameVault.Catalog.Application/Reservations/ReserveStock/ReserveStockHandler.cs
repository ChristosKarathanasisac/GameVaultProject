using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Application.Errors;
using GameVault.Catalog.Domain.Entities;
using GameVault.Contracts.Responses.Catalog;
using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Reservations.ReserveStock;

public sealed class ReserveStockHandler : IReserveStockHandler
{
    private const int ReservationTtlMinutes = 15;

    private readonly IProductRepository _productRepository;
    private readonly IStockReservationRepository _reservationRepository;

    public ReserveStockHandler(IProductRepository productRepository, IStockReservationRepository reservationRepository)
    {
        _productRepository = productRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<ReservationResponse>> HandleAsync(ReserveStockCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Quantity <= 0)
            return CatalogErrors.InvalidQuantity;

        var existing = await _reservationRepository.GetByOrderAndProductAsync(command.OrderId, command.ProductId, cancellationToken);
        if (existing is not null)
            return existing.ToReservationResponse();

        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            return CatalogErrors.ProductNotFound;

        if (product.Stock is null || product.Stock.AvailableStock < command.Quantity)
            return CatalogErrors.InsufficientStock;

        product.Stock.Decrease(command.Quantity);

        var reservation = StockReservation.Reserve(
            command.ProductId,
            command.OrderId,
            command.Quantity,
            DateTime.UtcNow.AddMinutes(ReservationTtlMinutes));

        await _reservationRepository.AddAsync(reservation, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return reservation.ToReservationResponse();
    }
}

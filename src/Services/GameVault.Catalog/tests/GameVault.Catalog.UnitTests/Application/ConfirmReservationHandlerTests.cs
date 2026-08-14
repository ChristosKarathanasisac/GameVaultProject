using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Application.Errors;
using GameVault.Catalog.Application.Reservations.ConfirmReservation;
using GameVault.Catalog.Domain.Entities;
using GameVault.Catalog.Domain.Enums;
using NSubstitute;

namespace GameVault.Catalog.UnitTests.Application;

public sealed class ConfirmReservationHandlerTests
{
    private readonly IStockReservationRepository _reservationRepository = Substitute.For<IStockReservationRepository>();
    private readonly ConfirmReservationHandler _handler;

    public ConfirmReservationHandlerTests()
    {
        _handler = new ConfirmReservationHandler(_reservationRepository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConfirmedResponse_OnHappyPath()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));
        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);

        var result = await _handler.HandleAsync(new ConfirmReservationCommand(orderId, productId));

        Assert.True(result.IsSuccess);
        Assert.Equal(ReservationStatus.Confirmed.ToString(), result.Value.Status);
        Assert.Equal(orderId, result.Value.OrderId);
        Assert.Equal(productId, result.Value.ProductId);
    }

    [Fact]
    public async Task HandleAsync_PersistsChange_OnHappyPath()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));
        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);

        await _handler.HandleAsync(new ConfirmReservationCommand(orderId, productId));

        await _reservationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsReservationNotFound_WhenNoReservationExists()
    {
        _reservationRepository.GetByOrderAndProductAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);

        var result = await _handler.HandleAsync(new ConfirmReservationCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.ReservationNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_DoesNotPersist_WhenReservationNotFound()
    {
        _reservationRepository.GetByOrderAndProductAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);

        await _handler.HandleAsync(new ConfirmReservationCommand(Guid.NewGuid(), Guid.NewGuid()));

        await _reservationRepository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task HandleAsync_ReturnsInvalidReservationTransition_WhenAlreadyConfirmed()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));
        reservation.Confirm();
        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);

        var result = await _handler.HandleAsync(new ConfirmReservationCommand(orderId, productId));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.InvalidReservationTransition, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsInvalidReservationTransition_WhenAlreadyReleased()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));
        reservation.Release();
        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);

        var result = await _handler.HandleAsync(new ConfirmReservationCommand(orderId, productId));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.InvalidReservationTransition, result.Error);
    }

    [Fact]
    public async Task HandleAsync_DoesNotPersist_WhenTransitionIsInvalid()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));
        reservation.Confirm();
        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);

        await _handler.HandleAsync(new ConfirmReservationCommand(orderId, productId));

        await _reservationRepository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}

using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Application.Errors;
using GameVault.Catalog.Application.Reservations.ReleaseReservation;
using GameVault.Catalog.Domain.Entities;
using GameVault.Catalog.Domain.Enums;
using NSubstitute;

namespace GameVault.Catalog.UnitTests.Application;

public sealed class ReleaseReservationHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IStockReservationRepository _reservationRepository = Substitute.For<IStockReservationRepository>();
    private readonly ReleaseReservationHandler _handler;

    public ReleaseReservationHandlerTests()
    {
        _handler = new ReleaseReservationHandler(_productRepository, _reservationRepository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsReleasedResponse_OnHappyPath()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));
        var product = CreateProductWithStock(productId, 5);

        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _handler.HandleAsync(new ReleaseReservationCommand(orderId, productId));

        Assert.True(result.IsSuccess);
        Assert.Equal(ReservationStatus.Released.ToString(), result.Value.Status);
        Assert.Equal(orderId, result.Value.OrderId);
        Assert.Equal(productId, result.Value.ProductId);
    }

    [Fact]
    public async Task HandleAsync_RestoresAvailableStock_OnHappyPath()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 4, DateTime.UtcNow.AddMinutes(15));
        var product = CreateProductWithStock(productId, 6);

        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        await _handler.HandleAsync(new ReleaseReservationCommand(orderId, productId));

        Assert.Equal(10, product.Stock!.AvailableStock);
    }

    [Fact]
    public async Task HandleAsync_PersistsChanges_OnHappyPath()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 2, DateTime.UtcNow.AddMinutes(15));
        var product = CreateProductWithStock(productId, 5);

        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);
        _productRepository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(product);

        await _handler.HandleAsync(new ReleaseReservationCommand(orderId, productId));

        await _productRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsReservationNotFound_WhenNoReservationExists()
    {
        _reservationRepository.GetByOrderAndProductAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);

        var result = await _handler.HandleAsync(new ReleaseReservationCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.ReservationNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_DoesNotPersist_WhenReservationNotFound()
    {
        _reservationRepository.GetByOrderAndProductAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StockReservation?)null);

        await _handler.HandleAsync(new ReleaseReservationCommand(Guid.NewGuid(), Guid.NewGuid()));

        await _productRepository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task HandleAsync_ReturnsInvalidReservationTransition_WhenAlreadyConfirmed()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));
        reservation.Confirm();
        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);

        var result = await _handler.HandleAsync(new ReleaseReservationCommand(orderId, productId));

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

        var result = await _handler.HandleAsync(new ReleaseReservationCommand(orderId, productId));

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.InvalidReservationTransition, result.Error);
    }

    [Fact]
    public async Task HandleAsync_DoesNotChangeStock_WhenTransitionIsInvalid()
    {
        var (orderId, productId) = (Guid.NewGuid(), Guid.NewGuid());
        var reservation = StockReservation.Reserve(productId, orderId, 3, DateTime.UtcNow.AddMinutes(15));
        reservation.Confirm();
        _reservationRepository.GetByOrderAndProductAsync(orderId, productId, Arg.Any<CancellationToken>())
            .Returns(reservation);

        await _handler.HandleAsync(new ReleaseReservationCommand(orderId, productId));

        await _productRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await _productRepository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static Product CreateProductWithStock(Guid productId, int availableStock)
    {
        var product = Product.Create("Test Game", null, 9.99m);
        var stock = Stock.Create(productId, availableStock);

        typeof(Product)
            .GetProperty(nameof(Product.Stock))!
            .SetValue(product, stock);

        return product;
    }
}

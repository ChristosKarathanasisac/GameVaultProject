using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Domain.Entities;
using GameVault.Catalog.Infrastructure.Persistence;
using GameVault.Catalog.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Catalog.IntegrationTests.Reservations;

[Collection(nameof(CatalogApiCollection))]
public sealed class StockReservationTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory;

    public StockReservationTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Reserve_DecreasesAvailableStockInDatabase()
    {
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 10);

        using var scope = _factory.Services.CreateScope();
        var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var reservationRepo = scope.ServiceProvider.GetRequiredService<IStockReservationRepository>();

        var product = await productRepo.GetByIdAsync(productId);
        product!.Stock!.Decrease(3);

        var reservation = StockReservation.Reserve(productId, Guid.NewGuid(), 3, DateTime.UtcNow.AddMinutes(15));
        await reservationRepo.AddAsync(reservation);
        await productRepo.SaveChangesAsync();

        Assert.Equal(7, await _factory.GetAvailableStockAsync(productId));
    }

    [Fact]
    public async Task Reserve_IsIdempotent_WhenSameOrderAndProductReservedTwice()
    {
        // Verifies the UX_StockReservations_OrderId_ProductId unique index enforces idempotency.
        var productId = await _factory.SeedProductWithStockAsync("Game", 9.99m, 10);
        var orderId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var existing = await db.StockReservations
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ProductId == productId);
        Assert.Null(existing);

        var reservation = StockReservation.Reserve(productId, orderId, 2, DateTime.UtcNow.AddMinutes(15));
        await db.StockReservations.AddAsync(reservation);
        await db.SaveChangesAsync();

        // Second insert for the same orderId+productId must violate the unique index.
        var duplicate = StockReservation.Reserve(productId, orderId, 2, DateTime.UtcNow.AddMinutes(15));
        await db.StockReservations.AddAsync(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Reserve_ThrowsDbUpdateConcurrencyException_OnConcurrentStockModification()
    {
        // Proves the xmin concurrency token on Stock prevents overselling under concurrent writes.
        // The losing writer's SaveChanges sees a stale xmin and EF throws DbUpdateConcurrencyException.
        var productId = await _factory.SeedProductWithStockAsync("Hot Game", 9.99m, 1);

        using var winnerScope = _factory.Services.CreateScope();
        var winnerDb = winnerScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var winnerStock = await winnerDb.Stock.FirstAsync(s => s.ProductId == productId);

        using var loserScope = _factory.Services.CreateScope();
        var loserDb = loserScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var loserStock = await loserDb.Stock.FirstAsync(s => s.ProductId == productId);

        // Winner decrements and saves first.
        winnerStock.Decrease(1);
        await winnerDb.SaveChangesAsync();

        // Loser tries to decrement the same row with a stale xmin — must fail.
        loserStock.Decrease(1);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => loserDb.SaveChangesAsync());

        // Stock should still be 0 (winner's decrement only), not -1.
        Assert.Equal(0, await _factory.GetAvailableStockAsync(productId));
    }
}

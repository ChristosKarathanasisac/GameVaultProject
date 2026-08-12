using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Catalog.IntegrationTests.Products;

[Collection(nameof(CatalogApiCollection))]
public sealed class ListProductsTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory;

    public ListProductsTests(CatalogApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ListAsync_ReturnsSeededProducts_WithCorrectCount()
    {
        await _factory.SeedProductWithStockAsync("Alpha", 9.99m, 5);
        await _factory.SeedProductWithStockAsync("Beta", 14.99m, 10);
        await _factory.SeedProductWithStockAsync("Gamma", 19.99m, 3);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var (items, totalCount) = await repository.ListAsync(1, 20);

        Assert.Equal(3, totalCount);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task ListAsync_ReturnsSortedByNameAscending()
    {
        await _factory.SeedProductWithStockAsync("Zephyr", 9.99m, 5);
        await _factory.SeedProductWithStockAsync("Alpha", 14.99m, 10);
        await _factory.SeedProductWithStockAsync("Mango", 19.99m, 3);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var (items, _) = await repository.ListAsync(1, 20);

        Assert.Equal("Alpha", items[0].Name);
        Assert.Equal("Mango", items[1].Name);
        Assert.Equal("Zephyr", items[2].Name);
    }

    [Fact]
    public async Task ListAsync_ExcludesSoftDeletedProducts()
    {
        // Verifies the IsDeleted filter is enforced at the database level.
        await _factory.SeedProductWithStockAsync("Active Game", 9.99m, 5);
        await _factory.SeedSoftDeletedProductAsync("Deleted Game", 14.99m, 10);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var (items, totalCount) = await repository.ListAsync(1, 20);

        Assert.Equal(1, totalCount);
        Assert.Equal("Active Game", items[0].Name);
    }

    [Fact]
    public async Task ListAsync_ReturnsCorrectPage_WhenResultsSpanMultiplePages()
    {
        for (var i = 1; i <= 7; i++)
            await _factory.SeedProductWithStockAsync($"Game {i:D2}", 9.99m, i);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var (page1Items, totalCount) = await repository.ListAsync(1, 5);
        var (page2Items, _) = await repository.ListAsync(2, 5);

        Assert.Equal(7, totalCount);
        Assert.Equal(5, page1Items.Count);
        Assert.Equal(2, page2Items.Count);
    }

    [Fact]
    public async Task ListAsync_IncludesStockNavigation()
    {
        await _factory.SeedProductWithStockAsync("Game With Stock", 9.99m, 42);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        var (items, _) = await repository.ListAsync(1, 20);

        Assert.NotNull(items[0].Stock);
        Assert.Equal(42, items[0].Stock!.AvailableStock);
    }
}

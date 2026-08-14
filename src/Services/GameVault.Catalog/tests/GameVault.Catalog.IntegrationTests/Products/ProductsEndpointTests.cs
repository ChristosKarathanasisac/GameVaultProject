using System.Net;
using System.Net.Http.Json;
using GameVault.Catalog.IntegrationTests.Fixtures;
using GameVault.Contracts.Responses;
using GameVault.Contracts.Responses.Catalog;

namespace GameVault.Catalog.IntegrationTests.Products;

[Collection(nameof(CatalogApiCollection))]
public sealed class ProductsEndpointTests : IAsyncLifetime
{
    private readonly CatalogApiFactory _factory;
    private readonly HttpClient _client;

    public ProductsEndpointTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_Returns200_WithEmptyPage_WhenCatalogIsEmpty()
    {
        var response = await _client.GetAsync("/api/products");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<GameResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
        Assert.Equal(0, body.TotalPages);
    }

    [Fact]
    public async Task List_Returns200_WithSeededProducts()
    {
        await _factory.SeedProductWithStockAsync("Alpha Game", 9.99m, 5);
        await _factory.SeedProductWithStockAsync("Beta Game", 14.99m, 10);

        var response = await _client.GetAsync("/api/products");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<GameResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(2, body.TotalCount);
        Assert.Equal(2, body.Items.Count);
    }

    [Fact]
    public async Task List_ExcludesSoftDeletedProducts()
    {
        await _factory.SeedProductWithStockAsync("Active Game", 9.99m, 5);
        await _factory.SeedSoftDeletedProductAsync("Deleted Game", 14.99m, 3);

        var response = await _client.GetAsync("/api/products");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<GameResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(1, body.TotalCount);
        Assert.Equal("Active Game", body.Items[0].Title);
    }

    [Fact]
    public async Task List_ReturnsPaginatedResults_WhenPageSizeIsSmall()
    {
        for (var i = 1; i <= 6; i++)
            await _factory.SeedProductWithStockAsync($"Game {i:D2}", 9.99m, i);

        var response = await _client.GetAsync("/api/products?page=1&pageSize=4");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<GameResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(6, body.TotalCount);
        Assert.Equal(4, body.Items.Count);
        Assert.Equal(2, body.TotalPages);
        Assert.Equal(1, body.Page);
        Assert.Equal(4, body.PageSize);
    }

    [Fact]
    public async Task List_ReturnsSecondPage_WithRemainingItems()
    {
        for (var i = 1; i <= 6; i++)
            await _factory.SeedProductWithStockAsync($"Game {i:D2}", 9.99m, i);

        var response = await _client.GetAsync("/api/products?page=2&pageSize=4");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<GameResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(2, body.Page);
    }

    [Fact]
    public async Task List_ReturnsCorrectStockQuantity_ForEachProduct()
    {
        await _factory.SeedProductWithStockAsync("Game With Stock", 9.99m, 42);

        var response = await _client.GetAsync("/api/products");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<GameResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(42, body.Items[0].StockQuantity);
    }

    [Fact]
    public async Task GetById_Returns200_WhenProductExists()
    {
        var productId = await _factory.SeedProductWithStockAsync("Single Game", 29.99m, 7);

        var response = await _client.GetAsync($"/api/products/{productId}");
        var body = await response.Content.ReadFromJsonAsync<GameResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(productId, body.Id);
        Assert.Equal("Single Game", body.Title);
        Assert.Equal(29.99m, body.Price);
        Assert.Equal(7, body.StockQuantity);
    }

    [Fact]
    public async Task GetById_Returns404_WhenProductDoesNotExist()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Returns404_WhenProductIsSoftDeleted()
    {
        var productId = await _factory.SeedSoftDeletedProductAsync("Deleted Game", 9.99m, 5);

        var response = await _client.GetAsync($"/api/products/{productId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

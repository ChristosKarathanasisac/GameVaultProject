using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Application.Products.ListProducts;
using GameVault.Catalog.Domain.Entities;
using NSubstitute;

namespace GameVault.Catalog.UnitTests.Application;

public sealed class ListProductsHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly ListProductsHandler _handler;

    public ListProductsHandlerTests()
    {
        _handler = new ListProductsHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyPage_WhenCatalogIsEmpty()
    {
        _repository.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Product>(), 0));

        var result = await _handler.HandleAsync(new ListProductsQuery(1, 20));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSinglePage_WhenAllItemsFitOnOnePage()
    {
        var products = CreateProducts(3);
        _repository.ListAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns((products, 3));

        var result = await _handler.HandleAsync(new ListProductsQuery(1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
    }

    [Fact]
    public async Task HandleAsync_CalculatesMultiplePages_WhenTotalExceedsPageSize()
    {
        var products = CreateProducts(5);
        _repository.ListAsync(1, 5, Arg.Any<CancellationToken>())
            .Returns((products, 15));

        var result = await _handler.HandleAsync(new ListProductsQuery(1, 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Items.Count);
        Assert.Equal(15, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnlyWhatRepositoryProvides_ReflectingDeletedProductFilter()
    {
        // Filtering of deleted products is enforced at the repository boundary (DB query filter).
        // The handler returns exactly what the repository gives it with no additional filtering.
        var products = CreateProducts(2);
        _repository.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((products, 2));

        var result = await _handler.HandleAsync(new ListProductsQuery(1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task HandleAsync_ClampsPageSizeToMaximum_WhenPageSizeExceedsHundred()
    {
        _repository.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Product>(), 0));

        await _handler.HandleAsync(new ListProductsQuery(1, 200));

        await _repository.Received(1).ListAsync(1, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ClampsPageSizeToDefault_WhenPageSizeIsZero()
    {
        _repository.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Product>(), 0));

        await _handler.HandleAsync(new ListProductsQuery(1, 0));

        await _repository.Received(1).ListAsync(1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ClampsPageSizeToDefault_WhenPageSizeIsNegative()
    {
        _repository.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Product>(), 0));

        await _handler.HandleAsync(new ListProductsQuery(1, -5));

        await _repository.Received(1).ListAsync(1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ClampsPageToOne_WhenPageIsZero()
    {
        _repository.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Product>(), 0));

        await _handler.HandleAsync(new ListProductsQuery(0, 20));

        await _repository.Received(1).ListAsync(1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ClampsPageToOne_WhenPageIsNegative()
    {
        _repository.ListAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Product>(), 0));

        await _handler.HandleAsync(new ListProductsQuery(-3, 20));

        await _repository.Received(1).ListAsync(1, 20, Arg.Any<CancellationToken>());
    }

    private static IReadOnlyList<Product> CreateProducts(int count) =>
        Enumerable.Range(1, count)
            .Select(i => Product.Create($"Game {i}", null, 9.99m))
            .ToList();
}

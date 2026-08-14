using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Application.Errors;
using GameVault.Catalog.Application.Products.GetProductById;
using GameVault.Catalog.Domain.Entities;
using GameVault.SharedKernel.Results;
using NSubstitute;

namespace GameVault.Catalog.UnitTests.Application;

public sealed class GetProductByIdHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly GetProductByIdHandler _handler;

    public GetProductByIdHandlerTests()
    {
        _handler = new GetProductByIdHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_ReturnsGameResponse_WhenProductExists()
    {
        var product = Product.Create("Halo", "Shooter", 29.99m);
        var stock = Stock.Create(product.Id, 10);
        typeof(Product).GetProperty(nameof(Product.Stock))!.SetValue(product, stock);

        _repository.GetByIdAsync(product.Id, default).Returns(product);

        var result = await _handler.HandleAsync(product.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(product.Id, result.Value.Id);
        Assert.Equal("Halo", result.Value.Title);
        Assert.Equal(29.99m, result.Value.Price);
        Assert.Equal(10, result.Value.StockQuantity);
    }

    [Fact]
    public async Task HandleAsync_ReturnsProductNotFound_WhenProductDoesNotExist()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), default).Returns((Product?)null);

        var result = await _handler.HandleAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogErrors.ProductNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZeroStock_WhenStockNavigationIsNull()
    {
        var product = Product.Create("Cyberpunk", null, 59.99m);
        _repository.GetByIdAsync(product.Id, default).Returns(product);

        var result = await _handler.HandleAsync(product.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.StockQuantity);
    }
}

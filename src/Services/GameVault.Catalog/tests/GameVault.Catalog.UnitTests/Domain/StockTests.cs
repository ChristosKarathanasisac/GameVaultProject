using GameVault.Catalog.Domain.Entities;
using GameVault.Catalog.Domain.Exceptions;

namespace GameVault.Catalog.UnitTests.Domain;

public sealed class StockTests
{
    [Fact]
    public void Decrease_ReducesAvailableStock_WhenQuantityIsValid()
    {
        var stock = Stock.Create(Guid.NewGuid(), 10);
        var before = DateTime.UtcNow;

        stock.Decrease(3);

        Assert.Equal(7, stock.AvailableStock);
        Assert.True(stock.UpdatedAt >= before);
    }

    [Fact]
    public void Decrease_ToExactlyZero_IsAllowed()
    {
        var stock = Stock.Create(Guid.NewGuid(), 5);

        stock.Decrease(5);

        Assert.Equal(0, stock.AvailableStock);
    }

    [Fact]
    public void Decrease_ThrowsInsufficientStockException_WhenQuantityExceedsAvailable()
    {
        var stock = Stock.Create(Guid.NewGuid(), 3);

        Assert.Throws<InsufficientStockException>(() => stock.Decrease(4));
    }

    [Fact]
    public void Decrease_DoesNotMutateStock_WhenItThrows()
    {
        var stock = Stock.Create(Guid.NewGuid(), 3);

        try { stock.Decrease(10); } catch (InsufficientStockException) { }

        Assert.Equal(3, stock.AvailableStock);
    }

    [Fact]
    public void Increase_AddsToAvailableStock()
    {
        var stock = Stock.Create(Guid.NewGuid(), 5);
        var before = DateTime.UtcNow;

        stock.Increase(7);

        Assert.Equal(12, stock.AvailableStock);
        Assert.True(stock.UpdatedAt >= before);
    }

    [Fact]
    public void Increase_ByZero_LeavesStockUnchanged()
    {
        var stock = Stock.Create(Guid.NewGuid(), 5);

        stock.Increase(0);

        Assert.Equal(5, stock.AvailableStock);
    }
}

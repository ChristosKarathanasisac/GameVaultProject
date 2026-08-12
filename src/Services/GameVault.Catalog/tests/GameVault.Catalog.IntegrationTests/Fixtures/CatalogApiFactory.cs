using GameVault.Catalog.Domain.Entities;
using GameVault.Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GameVault.Catalog.IntegrationTests.Fixtures;

public sealed class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<CatalogDbContext>(options =>
                options.UseNpgsql(
                    _postgres.GetConnectionString(),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "catalog")));
        });
    }

    // --- Database helpers ---

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.StockReservations.ExecuteDeleteAsync();
        await db.Stock.ExecuteDeleteAsync();
        await db.Products.ExecuteDeleteAsync();
    }

    public async Task<Guid> SeedProductWithStockAsync(string name, decimal price, int initialStock)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var product = Product.Create(name, null, price);
        var stock = Stock.Create(product.Id, initialStock);

        db.Products.Add(product);
        db.Stock.Add(stock);
        await db.SaveChangesAsync();

        return product.Id;
    }

    public async Task<Guid> SeedSoftDeletedProductAsync(string name, decimal price, int initialStock)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var product = Product.Create(name, null, price);
        product.SoftDelete();
        var stock = Stock.Create(product.Id, initialStock);

        db.Products.Add(product);
        db.Stock.Add(stock);
        await db.SaveChangesAsync();

        return product.Id;
    }

    public async Task<int> GetAvailableStockAsync(Guid productId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var stock = await db.Stock.FirstAsync(s => s.ProductId == productId);
        return stock.AvailableStock;
    }

    // --- Lifecycle ---

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

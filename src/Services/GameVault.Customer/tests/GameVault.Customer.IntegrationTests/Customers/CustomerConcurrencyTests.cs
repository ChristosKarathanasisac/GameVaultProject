using GameVault.Customer.Infrastructure.Persistence;
using GameVault.Customer.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Customer.IntegrationTests.Customers;

[Collection(nameof(CustomerApiCollection))]
public sealed class CustomerConcurrencyTests : IAsyncLifetime
{
    private readonly CustomerApiFactory _factory;

    public CustomerConcurrencyTests(CustomerApiFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SaveChangesAsync_ThrowsDbUpdateConcurrencyException_WhenRowChangedSinceLoad()
    {
        // Proves the xmin concurrency token (CustomerConfiguration) actually enforces
        // optimistic concurrency against real PostgreSQL, per CLAUDE.md §7b.
        var customerId = await _factory.SeedCustomerAsync("alice@example.com", "Alice", "Smith");

        using var staleScope = _factory.Services.CreateScope();
        var staleContext = staleScope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var staleCustomer = await staleContext.Customers.FirstAsync(c => c.Id == customerId);

        using (var winningScope = _factory.Services.CreateScope())
        {
            var winningContext = winningScope.ServiceProvider.GetRequiredService<CustomerDbContext>();
            var winningCustomer = await winningContext.Customers.FirstAsync(c => c.Id == customerId);
            winningCustomer.Update("Alicia", "Jones", null);
            await winningContext.SaveChangesAsync();
        }

        staleCustomer.Update("Conflicted", "Update", null);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());
    }
}

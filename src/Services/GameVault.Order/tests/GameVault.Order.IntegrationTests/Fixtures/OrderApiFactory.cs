using GameVault.Order.Application.Abstractions;
using GameVault.Order.IntegrationTests.Auth;
using GameVault.Order.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.IntegrationTests.Fixtures;

/// <summary>
/// Shared WebApplicationFactory for Order integration tests.
/// Starts one PostgreSQL container per test collection and replaces three production
/// registrations: the database (→ Testcontainers), the Catalog client (→ NSubstitute mock),
/// and the JWT auth scheme (→ TestAuthHandler).
/// Migrations run automatically on first server start (inherited from Program.cs startup).
/// </summary>
public sealed class OrderApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public ICatalogClient CatalogClientMock { get; } = Substitute.For<ICatalogClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ReplaceDatabase(services);
            ReplaceCatalogClient(services);
            ReplaceAuthentication(services);
        });
    }

    private void ReplaceDatabase(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(
                _postgres.GetConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "order")));
    }

    private void ReplaceCatalogClient(IServiceCollection services)
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(ICatalogClient))
            .ToList();

        foreach (var d in descriptors)
            services.Remove(d);

        services.AddSingleton(CatalogClientMock);
    }

    private static void ReplaceAuthentication(IServiceCollection services)
    {
        // PostConfigure runs after AddKeycloakAuthentication, overriding the default scheme
        // to our test handler without disturbing the authorization policies.
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
            options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            options.DefaultScheme = TestAuthHandler.SchemeName;
        });

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
    }

    // --- Client helpers ---

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }

    // --- Database helpers ---

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await db.OrderLines.ExecuteDeleteAsync();
        await db.Orders.ExecuteDeleteAsync();
    }

    public async Task<Guid> SeedOrderAsync(Guid customerId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var line = GameVault.Order.Domain.Entities.OrderLine.Create(
            Guid.NewGuid(), "Test Game", 29.99m, 1);
        var order = OrderEntity.Create(customerId, [line]);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order.Id;
    }

    // --- Lifecycle ---

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Infrastructure.Persistence;
using GameVault.Customer.IntegrationTests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using CustomerEntity = global::GameVault.Customer.Domain.Entities.Customer;

namespace GameVault.Customer.IntegrationTests.Fixtures;

/// <summary>
/// Shared WebApplicationFactory for Customer integration tests.
/// Starts one PostgreSQL container per test collection and replaces three production
/// registrations: the database (→ Testcontainers), Keycloak (→ NSubstitute mock),
/// and the JWT auth scheme (→ TestAuthHandler).
/// Migrations run automatically on first server start (inherited from Program.cs startup).
/// </summary>
public sealed class CustomerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public IKeycloakAdminClient KeycloakMock { get; } = Substitute.For<IKeycloakAdminClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ReplaceDatabase(services);
            ReplaceKeycloak(services);
            ReplaceAuthentication(services);
        });
    }

    private void ReplaceDatabase(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CustomerDbContext>));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddDbContext<CustomerDbContext>(options =>
            options.UseNpgsql(
                _postgres.GetConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "customer")));
    }

    private void ReplaceKeycloak(IServiceCollection services)
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(IKeycloakAdminClient))
            .ToList();

        foreach (var d in descriptors)
            services.Remove(d);

        services.AddSingleton(KeycloakMock);
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
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        await db.Customers.ExecuteDeleteAsync();
    }

    public async Task<Guid> SeedCustomerAsync(
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User",
        string? phoneNumber = null)
    {
        var id = Guid.NewGuid();
        var customer = CustomerEntity.Create(id, email, firstName, lastName, phoneNumber);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return id;
    }

    public async Task SoftDeleteCustomerAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = await db.Customers.FirstAsync(c => c.Id == id);
        customer.SoftDelete();
        await db.SaveChangesAsync();
    }

    // --- Lifecycle ---

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

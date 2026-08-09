using GameVault.Customer.Application.Abstractions;
using GameVault.Customer.Infrastructure.Keycloak;
using GameVault.Customer.Infrastructure.Persistence;
using GameVault.Customer.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Customer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CustomerDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("CustomerDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "customer")));

        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.Configure<KeycloakOptions>(options =>
            configuration.GetSection(KeycloakOptions.SectionName).Bind(options));
        services.AddSingleton<HttpClient>();
        services.AddScoped<IKeycloakAdminClient, KeycloakAdminClient>();

        return services;
    }
}

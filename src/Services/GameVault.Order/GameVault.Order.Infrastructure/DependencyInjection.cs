using GameVault.Order.Application.Abstractions;
using GameVault.Order.Infrastructure.Catalog;
using GameVault.Order.Infrastructure.Payment;
using GameVault.Order.Infrastructure.Persistence;
using GameVault.Order.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Order.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("OrderDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "order")));

        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddDaprClient();
        services.AddScoped<ICatalogClient, CatalogClient>();

        services.AddSingleton<Random>();
        services.AddScoped<IPaymentGateway, MockedPaymentGateway>();

        return services;
    }
}

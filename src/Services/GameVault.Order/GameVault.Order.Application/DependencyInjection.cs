using GameVault.Order.Application.PlaceOrder;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Order.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPlaceOrderHandler, PlaceOrderHandler>();
        return services;
    }
}

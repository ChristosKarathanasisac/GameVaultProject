using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Orders;
using GameVault.Order.Application.PayOrder;
using GameVault.Order.Application.PlaceOrder;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Order.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPlaceOrderHandler, PlaceOrderHandler>();
        services.AddScoped<IPayOrderHandler, PayOrderHandler>();
        services.AddScoped<IOrderCompensationService, OrderCompensationService>();
        return services;
    }
}

using GameVault.Notifications.Application.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderCompletedEventHandler, OrderCompletedEventHandler>();
        return services;
    }
}

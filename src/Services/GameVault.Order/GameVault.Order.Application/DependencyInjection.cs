using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Order.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}

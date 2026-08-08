using GameVault.Catalog.Application.Products.GetProductById;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGetProductByIdHandler, GetProductByIdHandler>();
        return services;
    }
}

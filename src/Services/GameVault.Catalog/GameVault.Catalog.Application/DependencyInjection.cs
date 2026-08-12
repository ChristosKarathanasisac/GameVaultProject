using GameVault.Catalog.Application.Products.GetProductById;
using GameVault.Catalog.Application.Products.ListProducts;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGetProductByIdHandler, GetProductByIdHandler>();
        services.AddScoped<IListProductsHandler, ListProductsHandler>();
        return services;
    }
}

using GameVault.Catalog.Application.Products.GetProductById;
using GameVault.Catalog.Application.Products.ListProducts;
using GameVault.Catalog.Application.Reservations.ConfirmReservation;
using GameVault.Catalog.Application.Reservations.ReleaseReservation;
using GameVault.Catalog.Application.Reservations.ReserveStock;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGetProductByIdHandler, GetProductByIdHandler>();
        services.AddScoped<IListProductsHandler, ListProductsHandler>();
        services.AddScoped<IReserveStockHandler, ReserveStockHandler>();
        services.AddScoped<IConfirmReservationHandler, ConfirmReservationHandler>();
        services.AddScoped<IReleaseReservationHandler, ReleaseReservationHandler>();
        return services;
    }
}

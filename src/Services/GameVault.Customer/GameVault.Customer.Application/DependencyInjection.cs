using GameVault.Customer.Application.Customers.Delete;
using GameVault.Customer.Application.Customers.GetById;
using GameVault.Customer.Application.Customers.Register;
using GameVault.Customer.Application.Customers.Update;
using Microsoft.Extensions.DependencyInjection;

namespace GameVault.Customer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRegisterCustomerHandler, RegisterCustomerHandler>();
        services.AddScoped<IGetCustomerByIdHandler, GetCustomerByIdHandler>();
        services.AddScoped<IUpdateCustomerHandler, UpdateCustomerHandler>();
        services.AddScoped<IDeleteCustomerHandler, DeleteCustomerHandler>();

        return services;
    }
}

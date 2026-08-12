using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.Errors;

public static class OrderErrors
{
    public static readonly Error OrderNotFound =
        Error.NotFound("Order.NotFound", "The order was not found.");
}

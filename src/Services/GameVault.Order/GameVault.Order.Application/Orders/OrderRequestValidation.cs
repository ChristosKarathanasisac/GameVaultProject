using GameVault.Order.Application.Errors;
using GameVault.Order.Application.PlaceOrder;
using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.Orders;

internal static class OrderRequestValidation
{
    public static Error? ValidateLines(IReadOnlyList<PlaceOrderLineCommand> lines)
    {
        if (lines.Count == 0)
            return OrderErrors.EmptyOrderLines;

        if (lines.Any(l => l.Quantity <= 0))
            return OrderErrors.InvalidQuantity;

        return null;
    }
}

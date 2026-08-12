using GameVault.SharedKernel.Results;

namespace GameVault.Order.Application.Errors;

public static class OrderErrors
{
    public static readonly Error OrderNotFound =
        Error.NotFound("Order.NotFound", "The order was not found.");

    public static readonly Error EmptyOrderLines =
        Error.Validation("Order.Validation.EmptyLines", "Order must contain at least one line.");

    public static readonly Error InvalidQuantity =
        Error.Validation("Order.Validation.InvalidQuantity", "All line quantities must be greater than zero.");

    public static readonly Error ProductNotFound =
        Error.NotFound("Order.ProductNotFound", "One or more products in the order were not found.");

    public static readonly Error PlacementFailed =
        Error.Conflict("Order.PlacementFailed", "The order could not be placed because stock reservation failed.");

    public static readonly Error InvalidPaymentState =
        Error.Conflict("Order.Payment.InvalidState", "Payment can only be initiated for orders in Reserved status.");

    public static readonly Error PaymentDeclined =
        Error.Conflict("Order.Payment.Declined", "The payment was declined.");
}

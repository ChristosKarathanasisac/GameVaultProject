using GameVault.SharedKernel.Results;

namespace GameVault.Catalog.Application.Errors;

public static class CatalogErrors
{
    public static readonly Error ProductNotFound =
        Error.NotFound("Catalog.ProductNotFound", "The product was not found.");

    public static readonly Error InsufficientStock =
        Error.Conflict("Catalog.InsufficientStock", "There is not enough stock available to fulfill this reservation.");

    public static readonly Error InvalidQuantity =
        Error.Validation("Catalog.InvalidQuantity", "Quantity must be greater than zero.");

    public static readonly Error ReservationNotFound =
        Error.NotFound("Catalog.ReservationNotFound", "No reservation exists for the given order and product.");

    public static readonly Error InvalidReservationTransition =
        Error.Conflict("Catalog.InvalidReservationTransition", "The reservation cannot be transitioned in its current status.");
}

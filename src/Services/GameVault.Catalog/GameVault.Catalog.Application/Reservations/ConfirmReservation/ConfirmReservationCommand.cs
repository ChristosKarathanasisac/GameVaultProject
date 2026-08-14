namespace GameVault.Catalog.Application.Reservations.ConfirmReservation;

public sealed record ConfirmReservationCommand(Guid OrderId, Guid ProductId);

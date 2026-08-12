namespace GameVault.Catalog.Application.Reservations.ReleaseReservation;

public sealed record ReleaseReservationCommand(Guid OrderId, Guid ProductId);

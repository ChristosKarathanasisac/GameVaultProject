using GameVault.Catalog.Domain.Entities;
using GameVault.Contracts.Responses.Catalog;

namespace GameVault.Catalog.Application.Reservations;

internal static class ReservationMapper
{
    internal static ReservationResponse ToReservationResponse(this StockReservation reservation) =>
        new(reservation.ProductId,
            reservation.OrderId,
            reservation.Quantity,
            reservation.Status.ToString(),
            reservation.ExpiresAt);
}

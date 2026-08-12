using GameVault.Catalog.Domain.Entities;
using GameVault.Catalog.Domain.Enums;
using GameVault.Catalog.Domain.Exceptions;

namespace GameVault.Catalog.UnitTests.Domain;

public sealed class StockReservationTests
{
    private static readonly DateTime ExpiresAt = DateTime.UtcNow.AddMinutes(15);

    [Fact]
    public void Reserve_CreatesReservationWithReservedStatus()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var reservation = StockReservation.Reserve(productId, orderId, 2, ExpiresAt);

        Assert.Equal(productId, reservation.ProductId);
        Assert.Equal(orderId, reservation.OrderId);
        Assert.Equal(2, reservation.Quantity);
        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
        Assert.Equal(ExpiresAt, reservation.ExpiresAt);
        Assert.True(reservation.CreatedAt >= before);
        Assert.True(reservation.UpdatedAt >= before);
    }

    [Fact]
    public void Confirm_FromReserved_Succeeds()
    {
        var reservation = StockReservation.Reserve(Guid.NewGuid(), Guid.NewGuid(), 1, ExpiresAt);
        var before = DateTime.UtcNow;

        reservation.Confirm();

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.True(reservation.UpdatedAt >= before);
    }

    [Fact]
    public void Confirm_FromConfirmed_ThrowsInvalidReservationStatusException()
    {
        var reservation = StockReservation.Reserve(Guid.NewGuid(), Guid.NewGuid(), 1, ExpiresAt);
        reservation.Confirm();

        Assert.Throws<InvalidReservationStatusException>(() => reservation.Confirm());
    }

    [Fact]
    public void Confirm_FromReleased_ThrowsInvalidReservationStatusException()
    {
        var reservation = StockReservation.Reserve(Guid.NewGuid(), Guid.NewGuid(), 1, ExpiresAt);
        reservation.Release();

        Assert.Throws<InvalidReservationStatusException>(() => reservation.Confirm());
    }

    [Fact]
    public void Release_FromReserved_Succeeds()
    {
        var reservation = StockReservation.Reserve(Guid.NewGuid(), Guid.NewGuid(), 1, ExpiresAt);
        var before = DateTime.UtcNow;

        reservation.Release();

        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.True(reservation.UpdatedAt >= before);
    }

    [Fact]
    public void Release_FromConfirmed_ThrowsInvalidReservationStatusException()
    {
        var reservation = StockReservation.Reserve(Guid.NewGuid(), Guid.NewGuid(), 1, ExpiresAt);
        reservation.Confirm();

        Assert.Throws<InvalidReservationStatusException>(() => reservation.Release());
    }

    [Fact]
    public void Release_FromReleased_ThrowsInvalidReservationStatusException()
    {
        var reservation = StockReservation.Reserve(Guid.NewGuid(), Guid.NewGuid(), 1, ExpiresAt);
        reservation.Release();

        Assert.Throws<InvalidReservationStatusException>(() => reservation.Release());
    }
}

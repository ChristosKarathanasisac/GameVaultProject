using GameVault.Order.Domain.Entities;
using GameVault.Order.Domain.Enums;
using GameVault.Order.Domain.Exceptions;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.UnitTests.Domain;

public sealed class OrderTests
{
    private static OrderEntity CreatePendingOrder()
    {
        var line = OrderLine.Create(Guid.NewGuid(), "Test Game", 49.99m, 2);
        return OrderEntity.Create(Guid.NewGuid(), [line]);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_CreatesOrderWithPendingStatusAndCorrectTotalAmount()
    {
        var customerId = Guid.NewGuid();
        var line = OrderLine.Create(Guid.NewGuid(), "Test Game", 29.99m, 2);
        var before = DateTime.UtcNow;

        var order = OrderEntity.Create(customerId, [line]);

        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(59.98m, order.TotalAmount);
        Assert.Single(order.Lines);
        Assert.True(order.CreatedAt >= before);
        Assert.True(order.UpdatedAt >= before);
    }

    [Fact]
    public void Create_WithMultipleLines_SumsTotalAmount()
    {
        var lines = new[]
        {
            OrderLine.Create(Guid.NewGuid(), "Game A", 10.00m, 3),
            OrderLine.Create(Guid.NewGuid(), "Game B", 20.00m, 1)
        };

        var order = OrderEntity.Create(Guid.NewGuid(), lines);

        Assert.Equal(50.00m, order.TotalAmount);
        Assert.Equal(2, order.Lines.Count);
    }

    // ── MarkReserved ──────────────────────────────────────────────────────────

    [Fact]
    public void MarkReserved_FromPending_Succeeds()
    {
        var order = CreatePendingOrder();
        var before = DateTime.UtcNow;

        order.MarkReserved();

        Assert.Equal(OrderStatus.Reserved, order.Status);
        Assert.True(order.UpdatedAt >= before);
    }

    [Fact]
    public void MarkReserved_FromReserved_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkReserved());
    }

    [Fact]
    public void MarkReserved_FromPaid_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();
        order.MarkPaid();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkReserved());
    }

    [Fact]
    public void MarkReserved_FromFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkReserved());
    }

    [Fact]
    public void MarkReserved_FromCompensationFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkCompensationFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkReserved());
    }

    // ── MarkPaid ──────────────────────────────────────────────────────────────

    [Fact]
    public void MarkPaid_FromReserved_Succeeds()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();
        var before = DateTime.UtcNow;

        order.MarkPaid();

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.True(order.UpdatedAt >= before);
    }

    [Fact]
    public void MarkPaid_FromPending_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkPaid());
    }

    [Fact]
    public void MarkPaid_FromPaid_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();
        order.MarkPaid();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkPaid());
    }

    [Fact]
    public void MarkPaid_FromFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkPaid());
    }

    [Fact]
    public void MarkPaid_FromCompensationFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkCompensationFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkPaid());
    }

    // ── MarkFailed ────────────────────────────────────────────────────────────

    [Fact]
    public void MarkFailed_FromPending_Succeeds()
    {
        var order = CreatePendingOrder();
        var before = DateTime.UtcNow;

        order.MarkFailed();

        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.True(order.UpdatedAt >= before);
    }

    [Fact]
    public void MarkFailed_FromReserved_Succeeds()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();
        var before = DateTime.UtcNow;

        order.MarkFailed();

        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.True(order.UpdatedAt >= before);
    }

    [Fact]
    public void MarkFailed_FromPaid_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();
        order.MarkPaid();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkFailed());
    }

    [Fact]
    public void MarkFailed_FromFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkFailed());
    }

    [Fact]
    public void MarkFailed_FromCompensationFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkCompensationFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkFailed());
    }

    // ── MarkPaymentFailed ─────────────────────────────────────────────────────

    [Fact]
    public void MarkPaymentFailed_FromReserved_Succeeds()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();
        var before = DateTime.UtcNow;

        order.MarkPaymentFailed();

        Assert.Equal(OrderStatus.PaymentFailed, order.Status);
        Assert.True(order.UpdatedAt >= before);
    }

    [Fact]
    public void MarkPaymentFailed_FromPending_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkPaymentFailed());
    }

    [Fact]
    public void MarkPaymentFailed_FromPaid_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();
        order.MarkPaid();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkPaymentFailed());
    }

    [Fact]
    public void MarkPaymentFailed_FromFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkPaymentFailed());
    }

    [Fact]
    public void MarkPaymentFailed_FromCompensationFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkCompensationFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkPaymentFailed());
    }

    // ── MarkCompensationFailed ────────────────────────────────────────────────

    [Fact]
    public void MarkCompensationFailed_FromPending_Succeeds()
    {
        var order = CreatePendingOrder();
        var before = DateTime.UtcNow;

        order.MarkCompensationFailed();

        Assert.Equal(OrderStatus.CompensationFailed, order.Status);
        Assert.True(order.UpdatedAt >= before);
    }

    [Fact]
    public void MarkCompensationFailed_FromReserved_Succeeds()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();
        var before = DateTime.UtcNow;

        order.MarkCompensationFailed();

        Assert.Equal(OrderStatus.CompensationFailed, order.Status);
        Assert.True(order.UpdatedAt >= before);
    }

    [Fact]
    public void MarkCompensationFailed_FromPaid_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkReserved();
        order.MarkPaid();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkCompensationFailed());
    }

    [Fact]
    public void MarkCompensationFailed_FromFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkCompensationFailed());
    }

    [Fact]
    public void MarkCompensationFailed_FromCompensationFailed_ThrowsInvalidOrderStatusException()
    {
        var order = CreatePendingOrder();
        order.MarkCompensationFailed();

        Assert.Throws<InvalidOrderStatusException>(() => order.MarkCompensationFailed());
    }
}

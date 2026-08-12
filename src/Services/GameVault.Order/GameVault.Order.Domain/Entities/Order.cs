using GameVault.Order.Domain.Enums;
using GameVault.Order.Domain.Exceptions;

namespace GameVault.Order.Domain.Entities;

public sealed class Order
{
    private Order() { }

    public Guid Id { get; private set; }

    // Deliberate Phase 1 decision — Order trusts the customer id from
    // the caller's JWT claims (validated by WebEdge) and does not call Customer to
    // verify the row exists or isn't soft-deleted. See TargetState.md decision log.
    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<OrderLine> Lines { get; private set; } = [];

    public static Order Create(Guid customerId, IEnumerable<OrderLine> lines)
    {
        var linesList = lines.ToList();
        return new Order
        {
            Id = Guid.CreateVersion7(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            TotalAmount = linesList.Sum(l => l.UnitPrice * l.Quantity),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lines = linesList
        };
    }

    public void MarkReserved()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOrderStatusException(Status, nameof(MarkReserved));

        Status = OrderStatus.Reserved;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaid()
    {
        if (Status != OrderStatus.Reserved)
            throw new InvalidOrderStatusException(Status, nameof(MarkPaid));

        Status = OrderStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status != OrderStatus.Pending && Status != OrderStatus.Reserved)
            throw new InvalidOrderStatusException(Status, nameof(MarkFailed));

        Status = OrderStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaymentFailed()
    {
        if (Status != OrderStatus.Reserved)
            throw new InvalidOrderStatusException(Status, nameof(MarkPaymentFailed));

        Status = OrderStatus.PaymentFailed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkCompensationFailed()
    {
        if (Status != OrderStatus.Pending && Status != OrderStatus.Reserved)
            throw new InvalidOrderStatusException(Status, nameof(MarkCompensationFailed));

        Status = OrderStatus.CompensationFailed;
        UpdatedAt = DateTime.UtcNow;
    }
}

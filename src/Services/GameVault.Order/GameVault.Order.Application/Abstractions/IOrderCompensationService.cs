namespace GameVault.Order.Application.Abstractions;

public interface IOrderCompensationService
{
    Task<bool> ReleaseReservationsAsync(
        Guid orderId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}

using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<OrderEntity> Items, int TotalCount)> ListByCustomerAsync(
        Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

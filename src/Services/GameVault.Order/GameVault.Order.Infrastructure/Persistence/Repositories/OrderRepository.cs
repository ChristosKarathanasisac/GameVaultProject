using GameVault.Order.Application.Abstractions;
using GameVault.Order.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OrderEntity = global::GameVault.Order.Domain.Entities.Order;

namespace GameVault.Order.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<OrderEntity> Items, int TotalCount)> ListByCustomerAsync(
        Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

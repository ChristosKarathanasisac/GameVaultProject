using GameVault.Catalog.Application.Abstractions;
using GameVault.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameVault.Catalog.Infrastructure.Persistence.Repositories;

public sealed class StockReservationRepository : IStockReservationRepository
{
    private readonly CatalogDbContext _context;

    public StockReservationRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<StockReservation?> GetByOrderAndProductAsync(Guid orderId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.StockReservations
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ProductId == productId, cancellationToken);
    }

    public async Task AddAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        await _context.StockReservations.AddAsync(reservation, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

namespace GameVault.Catalog.Application.Reservations.ReserveStock;

public sealed record ReserveStockCommand(Guid ProductId, Guid OrderId, int Quantity);

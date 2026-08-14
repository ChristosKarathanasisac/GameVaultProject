namespace GameVault.Contracts.Requests.Catalog;

public sealed record ReserveStockRequest(
    Guid OrderId,
    int Quantity);

namespace GameVault.Contracts.Responses.Catalog;

public sealed record ReservationResponse(
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    string Status,
    DateTime? ExpiresAt);

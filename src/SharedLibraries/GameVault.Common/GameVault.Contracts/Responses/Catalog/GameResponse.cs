namespace GameVault.Contracts.Responses.Catalog;

public sealed record GameResponse(
    Guid Id,
    string Title,
    string? Description,
    decimal Price,
    int StockQuantity);

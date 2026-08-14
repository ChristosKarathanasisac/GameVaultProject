namespace GameVault.Order.Domain.Entities;

public sealed class OrderLine
{
    private OrderLine() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductTitle { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public static OrderLine Create(Guid productId, string productTitle, decimal unitPrice, int quantity)
    {
        return new OrderLine
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            ProductTitle = productTitle,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}

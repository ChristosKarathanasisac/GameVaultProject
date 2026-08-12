namespace GameVault.Order.Domain.Enums;

public enum OrderStatus : byte
{
    Pending = 0,
    Reserved = 1,
    Paid = 2,
    Failed = 3,
    CompensationFailed = 4,
    Cancelled = 5
}

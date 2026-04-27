namespace Shared.Contracts.Events;

/// <summary>
/// Event DTO published by OrderService when a new order is created.
/// Consumed by InventoryService to deduct stock.
/// </summary>
public class OrderCreatedEvent
{
    public Guid EventId { get; set; }
    public Guid OrderId { get; set; }
    public string BranchId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new();
}

public class OrderItemEvent
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

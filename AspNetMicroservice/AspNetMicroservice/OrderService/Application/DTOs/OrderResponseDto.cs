namespace OrderService.Application.DTOs;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public string BranchId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsSynced { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class OrderItemResponseDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class SyncStatusDto
{
    public int TotalOrders { get; set; }
    public int SyncedCount { get; set; }
    public int UnsyncedCount { get; set; }
}

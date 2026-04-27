namespace OrderService.Application.DTOs;

public class CreateOrderDto
{
    public string BranchId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

namespace OrderService.Domain.Entities;

/// <summary>
/// Child entity of Order — represents one line item in a bill.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

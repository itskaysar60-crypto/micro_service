namespace OrderService.Domain.Entities;

/// <summary>
/// Aggregate Root — represents a customer bill/order at a branch.
/// </summary>
public class Order
{
    public Guid Id { get; set; }
    public string BranchId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsSynced { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();

    /// <summary>
    /// Factory method — creates a valid Order with calculated total.
    /// </summary>
    public static Order Create(string branchId, string customerName, List<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(branchId))
            throw new Exceptions.OrderValidationException("BranchId is required.");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new Exceptions.OrderValidationException("CustomerName is required.");
        if (items == null || items.Count == 0)
            throw new Exceptions.OrderValidationException("At least one item is required.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            CustomerName = customerName,
            IsSynced = false,
            CreatedAt = DateTime.UtcNow,
            Items = items
        };

        // Set OrderId on each item and calculate total
        foreach (var item in order.Items)
        {
            item.Id = Guid.NewGuid();
            item.OrderId = order.Id;
        }
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        return order;
    }

    public void MarkAsSynced()
    {
        IsSynced = true;
    }
}

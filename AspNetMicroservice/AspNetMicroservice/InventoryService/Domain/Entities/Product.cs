namespace InventoryService.Domain.Entities;

/// <summary>
/// Product entity with stock tracking.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Deducts stock. Throws if insufficient.
    /// </summary>
    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new Exceptions.InsufficientStockException("Quantity must be positive.");
        if (StockQuantity < quantity)
            throw new Exceptions.InsufficientStockException(
                $"Insufficient stock for {Name}. Available: {StockQuantity}, Requested: {quantity}");

        StockQuantity -= quantity;
    }
}

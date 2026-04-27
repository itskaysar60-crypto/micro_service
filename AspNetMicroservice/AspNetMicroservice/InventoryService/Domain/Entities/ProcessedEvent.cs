namespace InventoryService.Domain.Entities;

/// <summary>
/// Tracks processed event IDs for idempotency.
/// Prevents duplicate stock deductions.
/// </summary>
public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

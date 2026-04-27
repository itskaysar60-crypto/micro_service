namespace OrderService.Domain.Entities;

/// <summary>
/// Outbox event entity — part of Transactional Outbox Pattern.
/// Stored in the same DB transaction as the Order.
/// </summary>
public class OutboxEvent
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
}

using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

/// <summary>
/// Repository for Outbox events — part of Transactional Outbox Pattern.
/// </summary>
public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent outboxEvent);
    Task<List<OutboxEvent>> GetUnpublishedAsync();
    Task MarkPublishedAsync(Guid eventId);
    Task IncrementRetryAsync(Guid eventId, string error);
    Task SaveChangesAsync();
}

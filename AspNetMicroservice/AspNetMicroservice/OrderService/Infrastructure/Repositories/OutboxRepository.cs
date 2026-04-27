using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly OrderDbContext _db;

    public OutboxRepository(OrderDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(OutboxEvent outboxEvent)
        => await _db.OutboxEvents.AddAsync(outboxEvent);

    public async Task<List<OutboxEvent>> GetUnpublishedAsync()
    {
        return await _db.OutboxEvents
            .Where(e => !e.IsPublished && e.RetryCount < 10)
            .OrderBy(e => e.CreatedAt)
            .Take(20) // Process in batches
            .ToListAsync();
    }

    public async Task MarkPublishedAsync(Guid eventId)
    {
        var evt = await _db.OutboxEvents.FindAsync(eventId);
        if (evt != null)
        {
            evt.IsPublished = true;
            evt.PublishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task IncrementRetryAsync(Guid eventId, string error)
    {
        var evt = await _db.OutboxEvents.FindAsync(eventId);
        if (evt != null)
        {
            evt.RetryCount++;
            evt.LastError = error;
            await _db.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}

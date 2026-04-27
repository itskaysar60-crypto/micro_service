using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Persistence;

namespace InventoryService.Infrastructure.Repositories;

public class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly InventoryDbContext _db;

    public ProcessedEventRepository(InventoryDbContext db) => _db = db;

    public async Task<bool> ExistsAsync(Guid eventId)
        => await _db.ProcessedEvents.AnyAsync(e => e.EventId == eventId);

    public async Task AddAsync(Guid eventId)
    {
        await _db.ProcessedEvents.AddAsync(new ProcessedEvent
        {
            EventId = eventId,
            ProcessedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}

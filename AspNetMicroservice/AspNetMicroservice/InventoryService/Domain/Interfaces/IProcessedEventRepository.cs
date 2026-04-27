namespace InventoryService.Domain.Interfaces;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(Guid eventId);
    Task AddAsync(Guid eventId);
    Task SaveChangesAsync();
}

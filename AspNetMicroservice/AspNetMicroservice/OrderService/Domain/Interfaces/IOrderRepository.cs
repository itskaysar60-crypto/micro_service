using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

/// <summary>
/// Repository interface for Order aggregate.
/// Implementation lives in Infrastructure — Domain only defines the contract.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<List<Order>> GetAllAsync();
    Task<List<Order>> GetUnsyncedAsync();
    Task<int> GetSyncedCountAsync();
    Task<int> GetUnsyncedCountAsync();
    Task AddAsync(Order order);
    Task MarkAsSyncedAsync(Guid orderId);
    Task SaveChangesAsync();
}

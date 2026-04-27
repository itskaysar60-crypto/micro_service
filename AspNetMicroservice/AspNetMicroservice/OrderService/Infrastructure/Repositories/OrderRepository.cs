using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;

    public OrderRepository(OrderDbContext db)
    {
        _db = db;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Order>> GetUnsyncedAsync()
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Where(o => !o.IsSynced)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetSyncedCountAsync()
        => await _db.Orders.CountAsync(o => o.IsSynced);

    public async Task<int> GetUnsyncedCountAsync()
        => await _db.Orders.CountAsync(o => !o.IsSynced);

    public async Task AddAsync(Order order)
        => await _db.Orders.AddAsync(order);

    public async Task MarkAsSyncedAsync(Guid orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.MarkAsSynced();
        }
    }

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}

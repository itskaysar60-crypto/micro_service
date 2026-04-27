using InventoryService.Domain.Entities;

namespace InventoryService.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Product>> GetAllAsync();
    Task AddAsync(Product product);
    Task SaveChangesAsync();
}

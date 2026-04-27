using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Persistence;

namespace InventoryService.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly InventoryDbContext _db;

    public ProductRepository(InventoryDbContext db) => _db = db;

    public async Task<Product?> GetByIdAsync(Guid id)
        => await _db.Products.FindAsync(id);

    public async Task<List<Product>> GetAllAsync()
        => await _db.Products.OrderBy(p => p.Name).ToListAsync();

    public async Task AddAsync(Product product)
        => await _db.Products.AddAsync(product);

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}

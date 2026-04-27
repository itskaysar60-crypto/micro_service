using InventoryService.Application.DTOs;
using Shared.Contracts.Events;

namespace InventoryService.Application.Services;

/// <summary>
/// Application service interface for Inventory operations.
/// </summary>
public interface IInventoryService
{
    Task<Guid> CreateProductAsync(CreateProductDto dto);
    Task<List<ProductDto>> GetAllProductsAsync();
    Task<StockInfoDto?> GetStockAsync(Guid productId);
    Task ProcessSyncedOrderAsync(OrderCreatedEvent orderEvent);
}

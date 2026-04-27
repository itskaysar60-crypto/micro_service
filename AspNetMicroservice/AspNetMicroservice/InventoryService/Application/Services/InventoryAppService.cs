using InventoryService.Application.DTOs;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Interfaces;
using Shared.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.Services;

public class InventoryAppService : IInventoryService
{
    private readonly IProductRepository _productRepo;
    private readonly IProcessedEventRepository _processedEventRepo;
    private readonly ILogger<InventoryAppService> _logger;

    public InventoryAppService(
        IProductRepository productRepo,
        IProcessedEventRepository processedEventRepo,
        ILogger<InventoryAppService> logger)
    {
        _productRepo = productRepo;
        _processedEventRepo = processedEventRepo;
        _logger = logger;
    }

    public async Task<Guid> CreateProductAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            SKU = dto.SKU,
            StockQuantity = dto.StockQuantity,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepo.AddAsync(product);
        await _productRepo.SaveChangesAsync();
        return product.Id;
    }

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        var products = await _productRepo.GetAllAsync();
        return products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            StockQuantity = p.StockQuantity,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<StockInfoDto?> GetStockAsync(Guid productId)
    {
        var product = await _productRepo.GetByIdAsync(productId);
        if (product == null) return null;

        return new StockInfoDto
        {
            ProductId = product.Id,
            Name = product.Name,
            StockQuantity = product.StockQuantity
        };
    }

    /// <summary>
    /// Processes a synced order from OrderService.
    /// Idempotent — skips if EventId already processed.
    /// </summary>
    public async Task ProcessSyncedOrderAsync(OrderCreatedEvent orderEvent)
    {
        // ── Idempotency check ──
        if (await _processedEventRepo.ExistsAsync(orderEvent.EventId))
        {
            _logger.LogInformation("Event {EventId} already processed. Skipping.", orderEvent.EventId);
            return;
        }

        // ── Deduct stock for each item ──
        foreach (var item in orderEvent.Items)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found. Skipping stock deduction.", item.ProductId);
                continue;
            }

            product.DeductStock(item.Quantity);
            _logger.LogInformation(
                "Deducted {Qty} from product {Name}. Remaining: {Stock}",
                item.Quantity, product.Name, product.StockQuantity);
        }

        // ── Record this event as processed ──
        await _processedEventRepo.AddAsync(orderEvent.EventId);
        await _productRepo.SaveChangesAsync();
    }
}

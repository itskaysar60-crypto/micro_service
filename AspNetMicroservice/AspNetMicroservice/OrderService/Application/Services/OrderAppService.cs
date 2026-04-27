using System.Text.Json;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using Shared.Contracts.Events;

namespace OrderService.Application.Services;

/// <summary>
/// Application service — contains business logic for Order operations.
/// Depends on repository interfaces (DIP), not on EF Core directly.
/// </summary>
public class OrderAppService : IOrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly IOutboxRepository _outboxRepo;

    public OrderAppService(IOrderRepository orderRepo, IOutboxRepository outboxRepo)
    {
        _orderRepo = orderRepo;
        _outboxRepo = outboxRepo;
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderDto dto)
    {
        // Map DTO → Domain entities
        var items = dto.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        // Create Order via factory method (validation inside)
        var order = Order.Create(dto.BranchId, dto.CustomerName, items);

        // Create OutboxEvent in the same transaction (Outbox Pattern)
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            EventType = "OrderCreated",
            Payload = JsonSerializer.Serialize(new OrderCreatedEvent
            {
                EventId = Guid.NewGuid(),
                OrderId = order.Id,
                BranchId = order.BranchId,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }),
            IsPublished = false,
            CreatedAt = DateTime.UtcNow
        };

        // Atomic — both saved in one SaveChanges call
        await _orderRepo.AddAsync(order);
        await _outboxRepo.AddAsync(outboxEvent);
        await _orderRepo.SaveChangesAsync();

        return order.Id;
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(Guid id)
    {
        var order = await _orderRepo.GetByIdAsync(id);
        if (order == null) return null;
        return MapToDto(order);
    }

    public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
    {
        var orders = await _orderRepo.GetAllAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderResponseDto>> GetUnsyncedOrdersAsync()
    {
        var orders = await _orderRepo.GetUnsyncedAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<SyncStatusDto> GetSyncStatusAsync()
    {
        var synced = await _orderRepo.GetSyncedCountAsync();
        var unsynced = await _orderRepo.GetUnsyncedCountAsync();
        return new SyncStatusDto
        {
            TotalOrders = synced + unsynced,
            SyncedCount = synced,
            UnsyncedCount = unsynced
        };
    }

    // ── Private helper ──
    private static OrderResponseDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        BranchId = order.BranchId,
        CustomerName = order.CustomerName,
        TotalAmount = order.TotalAmount,
        IsSynced = order.IsSynced,
        CreatedAt = order.CreatedAt,
        Items = order.Items.Select(i => new OrderItemResponseDto
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}

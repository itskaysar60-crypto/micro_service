using OrderService.Application.DTOs;

namespace OrderService.Application.Services;

/// <summary>
/// Application service interface for Order operations.
/// Controller depends on this — not on concrete class (DIP).
/// </summary>
public interface IOrderService
{
    Task<Guid> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderResponseDto?> GetOrderByIdAsync(Guid id);
    Task<List<OrderResponseDto>> GetAllOrdersAsync();
    Task<List<OrderResponseDto>> GetUnsyncedOrdersAsync();
    Task<SyncStatusDto> GetSyncStatusAsync();
}

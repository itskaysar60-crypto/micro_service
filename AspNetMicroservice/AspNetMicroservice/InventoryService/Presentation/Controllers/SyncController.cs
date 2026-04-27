using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.Services;
using Shared.Contracts.Events;

namespace InventoryService.Presentation.Controllers;

/// <summary>
/// Receives synced orders from OrderService's HttpOutboxRelay.
/// This is the HTTP endpoint that replaces RabbitMQ consumer.
/// </summary>
[ApiController]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(IInventoryService inventoryService, ILogger<SyncController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/sync/orders — receives OrderCreatedEvent from OrderService.
    /// Idempotent — safe to call multiple times with the same EventId.
    /// </summary>
    [HttpPost("orders")]
    public async Task<IActionResult> SyncOrder([FromBody] OrderCreatedEvent orderEvent)
    {
        try
        {
            _logger.LogInformation(
                "Received sync for Order {OrderId} from Branch {BranchId}",
                orderEvent.OrderId, orderEvent.BranchId);

            await _inventoryService.ProcessSyncedOrderAsync(orderEvent);

            return Ok(new { message = "Order synced successfully.", eventId = orderEvent.EventId });
        }
        catch (Domain.Exceptions.InsufficientStockException ex)
        {
            _logger.LogWarning("Stock issue during sync: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing synced order.");
            return StatusCode(500, new { error = "Internal error during sync." });
        }
    }
}

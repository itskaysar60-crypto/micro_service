using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Services;

namespace OrderService.Presentation.Controllers;


[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IValidator<CreateOrderDto> _createOrderValidator;


    public OrdersController(IOrderService orderService, IValidator<CreateOrderDto> createOrderValidator)
    {
        _orderService = orderService;
        _createOrderValidator = createOrderValidator;
    }

    /// <summary>
    /// Create a new bill/order. Works offline — saved locally with IsSynced=false.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var validation = await _createOrderValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(new { errors });
        }

        try
        {
            var orderId = await _orderService.CreateOrderAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = orderId }, new
            {
                orderId,
                message = "Bill created successfully. Will sync when online."
            });
        }
        catch (Domain.Exceptions.OrderValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all orders.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Get a single order by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound(new { error = "Order not found." });
        return Ok(order);
    }

    /// <summary>
    /// Get all orders that haven't been synced to InventoryService yet.
    /// </summary>
    [HttpGet("unsynced")]
    public async Task<IActionResult> GetUnsynced()
    {
        var orders = await _orderService.GetUnsyncedOrdersAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Get sync status — how many orders are synced vs unsynced.
    /// </summary>
    [HttpGet("sync-status")]
    public async Task<IActionResult> GetSyncStatus()
    {
        var status = await _orderService.GetSyncStatusAsync();
        return Ok(status);
    }

  
}

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
    private readonly IExternalApiService _externalApi;

    public OrdersController(IOrderService orderService, IExternalApiService externalApi)
    {
        _orderService = orderService;
        _externalApi = externalApi;
    }

    /// <summary>
    /// Create a new bill/order. Works offline — saved locally with IsSynced=false.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
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

    // ── External API pull (RestSharp + JWT) ──────────────────────────────────

    /// <summary>
    /// Pulls data from an external API using the caller's JWT bearer token.
    /// The token is forwarded as-is — no re-authentication needed.
    ///
    /// Example request:
    ///   GET /api/v1/orders/external-pull?url=https://api.example.com/products
    ///   Authorization: Bearer eyJhbGci...
    /// </summary>
    /// <param name="url">Absolute URL of the remote endpoint to call.</param>
    [HttpGet("external-pull")]
    public async Task<IActionResult> PullFromExternalApi([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "'url' query parameter is required." });

        // Extract the raw token from the Authorization header.
        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { error = "A valid Bearer token is required." });

        var jwtToken = authHeader["Bearer ".Length..].Trim();

        try
        {
            // T = object so the raw JSON is returned without a fixed schema.
            // Replace with a concrete DTO when you know the shape of the response.
            var result = await _externalApi.GetAsync<object>(url, jwtToken, HttpContext.RequestAborted);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = "External API call failed.", detail = ex.Message });
        }
    }
}

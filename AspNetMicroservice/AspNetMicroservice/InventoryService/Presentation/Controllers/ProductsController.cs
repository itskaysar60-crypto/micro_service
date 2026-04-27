using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.DTOs;
using InventoryService.Application.Services;

namespace InventoryService.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public ProductsController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Create a new product with initial stock.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var productId = await _inventoryService.CreateProductAsync(dto);
        return CreatedAtAction(nameof(GetStock), new { id = productId }, new
        {
            productId,
            message = "Product created successfully."
        });
    }

    /// <summary>
    /// List all products with stock levels.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _inventoryService.GetAllProductsAsync();
        return Ok(products);
    }

    /// <summary>
    /// Get stock level for a specific product.
    /// </summary>
    [HttpGet("{id:guid}/stock")]
    public async Task<IActionResult> GetStock(Guid id)
    {
        var stock = await _inventoryService.GetStockAsync(id);
        if (stock == null) return NotFound(new { error = "Product not found." });
        return Ok(stock);
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Inventory;

namespace Empire.API.Controllers;

/// <summary>
/// Inventory management endpoints
/// </summary>
[Authorize]
public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get inventory items with filtering
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>List of inventory items</returns>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventory([FromBody] InventoryFilterRequest filter)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            filter.ShopId = shopId;
            var items = await _inventoryService.GetInventoryAsync(filter);
            return SuccessResponse(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory items");
            return ErrorResponse("Error retrieving inventory items", 500);
        }
    }

    /// <summary>
    /// Get all inventory items for a shop (supports query-string filtering)
    /// </summary>
    /// <param name="shopId">Shop ID (optional – falls back to JWT claim)</param>
    /// <param name="search">Optional search term</param>
    /// <param name="isActive">Filter by active status</param>
    /// <returns>List of inventory items</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllInventory(
        [FromQuery] int? shopId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive)
    {
        try
        {
            var resolvedShopId = shopId ?? GetCurrentShopId();
            if (resolvedShopId == 0)
                return UnauthorizedResponse("No shop selected");

            var filter = new InventoryFilterRequest
            {
                ShopId = resolvedShopId,
                SearchTerm = search
            };

            var items = await _inventoryService.GetInventoryAsync(filter);
            return SuccessResponse(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory items");
            return ErrorResponse("Error retrieving inventory items", 500);
        }
    }

    /// <summary>
    /// Get inventory item by ID
    /// </summary>
    /// <param name="id">Inventory ID</param>
    /// <returns>Inventory item details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInventory(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var item = await _inventoryService.GetInventoryByIdAsync(id, shopId);

            if (item == null)
                return NotFoundResponse("Inventory item not found");

            return SuccessResponse(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory item {ItemId}", id);
            return ErrorResponse("Error retrieving inventory item", 500);
        }
    }

    /// <summary>
    /// Create new inventory
    /// </summary>
    /// <param name="request">Inventory details</param>
    /// <returns>Created inventory</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInventory([FromBody] CreateInventoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            request.ShopId = shopId;
            var item = await _inventoryService.CreateInventoryAsync(request);

            _logger.LogInformation("Inventory created: {ItemId}", item.Id);

            return StatusCode(201, new
            {
                success = true,
                message = "Inventory created successfully",
                data = item,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory");
            return ErrorResponse("Error creating inventory", 500);
        }
    }

    /// <summary>
    /// Update inventory
    /// </summary>
    /// <param name="id">Inventory ID</param>
    /// <param name="request">Updated inventory details</param>
    /// <returns>Updated inventory</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateInventory(int id, [FromBody] UpdateInventoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var item = await _inventoryService.UpdateInventoryAsync(id, request);

            if (item == null)
                return NotFoundResponse("Inventory not found");

            _logger.LogInformation("Inventory updated: {ItemId}", id);

            return SuccessResponse(item, "Inventory updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory {ItemId}", id);
            return ErrorResponse("Error updating inventory", 500);
        }
    }

    /// <summary>
    /// Delete inventory
    /// </summary>
    /// <param name="id">Inventory ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInventory(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var success = await _inventoryService.DeleteInventoryAsync(id, shopId);

            if (!success)
                return NotFoundResponse("Inventory not found");

            _logger.LogInformation("Inventory deleted: {ItemId}", id);

            return SuccessResponse("Inventory deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory {ItemId}", id);
            return ErrorResponse("Error deleting inventory", 500);
        }
    }

    /// <summary>
    /// Adjust inventory quantity
    /// </summary>
    /// <param name="request">Adjustment details</param>
    /// <returns>Success message</returns>
    [HttpPost("adjust")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdjustInventory([FromBody] InventoryAdjustmentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var userId = GetCurrentUserId();
            await _inventoryService.AdjustInventoryAsync(request, userId);

            _logger.LogInformation("Inventory adjusted for item {ItemId} by user {UserId}", request.InventoryId, userId);

            return SuccessResponse("Inventory adjusted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting inventory");
            return ErrorResponse("Error adjusting inventory", 500);
        }
    }

    /// <summary>
    /// Get low stock items
    /// </summary>
    /// <returns>List of low stock items</returns>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockItems()
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var items = await _inventoryService.GetLowStockItemsAsync(shopId);
            return SuccessResponse(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock items");
            return ErrorResponse("Error retrieving low stock items", 500);
        }
    }

    /// <summary>
    /// Get all brands
    /// </summary>
    /// <returns>List of brands</returns>
    [HttpGet("brands")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrands()
    {
        try
        {
            // Sample brands - replace with actual database query
            var brands = new[]
            {
                new { id = 1, name = "Apple", icon = "apple" },
                new { id = 2, name = "Samsung", icon = "samsung" },
                new { id = 3, name = "Google", icon = "android" },
                new { id = 4, name = "OnePlus", icon = "phone_android" },
                new { id = 5, name = "Xiaomi", icon = "phone_android" },
                new { id = 6, name = "Huawei", icon = "phone_android" }
            };

            return SuccessResponse(brands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brands");
            return ErrorResponse("Error retrieving brands", 500);
        }
    }

    /// <summary>
    /// Get categories by brand
    /// </summary>
    /// <param name="brandId">Brand ID</param>
    /// <returns>List of categories</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories([FromQuery] int? brandId)
    {
        try
        {
            // Sample categories - replace with actual database query
            var categories = new[]
            {
                new { id = 1, name = "iPhone", icon = "smartphone", brandId = 1 },
                new { id = 2, name = "iPad", icon = "tablet", brandId = 1 },
                new { id = 3, name = "MacBook", icon = "laptop", brandId = 1 },
                new { id = 4, name = "Watch", icon = "watch", brandId = 1 },
                new { id = 5, name = "Galaxy S", icon = "smartphone", brandId = 2 },
                new { id = 6, name = "Galaxy Note", icon = "smartphone", brandId = 2 },
                new { id = 7, name = "Galaxy Z", icon = "smartphone", brandId = 2 }
            };

            var filtered = brandId.HasValue 
                ? categories.Where(c => c.brandId == brandId.Value)
                : categories;

            return SuccessResponse(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return ErrorResponse("Error retrieving categories", 500);
        }
    }

    /// <summary>
    /// Get models by brand and category
    /// </summary>
    /// <param name="brandId">Brand ID</param>
    /// <param name="categoryId">Category ID</param>
    /// <returns>List of models</returns>
    [HttpGet("models")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModels([FromQuery] int? brandId, [FromQuery] int? categoryId)
    {
        try
        {
            // Sample models - replace with actual database query
            var models = new[]
            {
                new { id = 1, name = "iPhone 14 Pro Max", brandId = 1, categoryId = 1 },
                new { id = 2, name = "iPhone 14 Pro", brandId = 1, categoryId = 1 },
                new { id = 3, name = "iPhone 14", brandId = 1, categoryId = 1 },
                new { id = 4, name = "iPhone 13 Pro Max", brandId = 1, categoryId = 1 },
                new { id = 5, name = "iPhone 13 Pro", brandId = 1, categoryId = 1 },
                new { id = 6, name = "iPhone 13", brandId = 1, categoryId = 1 },
                new { id = 7, name = "iPad Pro 12.9\"", brandId = 1, categoryId = 2 },
                new { id = 8, name = "iPad Air", brandId = 1, categoryId = 2 },
                new { id = 9, name = "Galaxy S23 Ultra", brandId = 2, categoryId = 5 },
                new { id = 10, name = "Galaxy S23+", brandId = 2, categoryId = 5 }
            };

            var filtered = models.AsEnumerable();
            if (brandId.HasValue)
                filtered = filtered.Where(m => m.brandId == brandId.Value);
            if (categoryId.HasValue)
                filtered = filtered.Where(m => m.categoryId == categoryId.Value);

            return SuccessResponse(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models");
            return ErrorResponse("Error retrieving models", 500);
        }
    }

    /// <summary>
    /// Get parts filtered by brand, category, and model
    /// </summary>
    /// <param name="brandId">Brand ID</param>
    /// <param name="categoryId">Category ID</param>
    /// <param name="modelId">Model ID</param>
    /// <returns>List of parts</returns>
    [HttpGet("parts")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParts([FromQuery] int? brandId, [FromQuery] int? categoryId, [FromQuery] int? modelId)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            // Sample parts - replace with actual database query from inventory
            var parts = new[]
            {
                new { id = 1, name = "LCD Screen", price = 89.99m, stock = 15, brandId = 1, categoryId = 1, modelId = 1 },
                new { id = 2, name = "Battery", price = 29.99m, stock = 25, brandId = 1, categoryId = 1, modelId = 1 },
                new { id = 3, name = "Back Glass", price = 39.99m, stock = 10, brandId = 1, categoryId = 1, modelId = 1 },
                new { id = 4, name = "Camera Module", price = 49.99m, stock = 8, brandId = 1, categoryId = 1, modelId = 1 },
                new { id = 5, name = "Charging Port", price = 19.99m, stock = 20, brandId = 1, categoryId = 1, modelId = 1 },
                new { id = 6, name = "Speaker", price = 24.99m, stock = 12, brandId = 1, categoryId = 1, modelId = 1 },
                new { id = 7, name = "LCD Screen", price = 79.99m, stock = 18, brandId = 1, categoryId = 1, modelId = 2 },
                new { id = 8, name = "Battery", price = 29.99m, stock = 22, brandId = 1, categoryId = 1, modelId = 2 }
            };

            var filtered = parts.AsEnumerable();
            if (brandId.HasValue)
                filtered = filtered.Where(p => p.brandId == brandId.Value);
            if (categoryId.HasValue)
                filtered = filtered.Where(p => p.categoryId == categoryId.Value);
            if (modelId.HasValue)
                filtered = filtered.Where(p => p.modelId == modelId.Value);

            return SuccessResponse(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving parts");
            return ErrorResponse("Error retrieving parts", 500);
        }
    }
}

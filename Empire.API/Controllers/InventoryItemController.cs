using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Inventory;

namespace Empire.API.Controllers;

/// <summary>
/// InventoryItem CRUD – uses the InventoryItems table (BrandId, DeviceCategoryId, DeviceModelId, SKU, CurrentStock, …)
/// </summary>
[Authorize]
[Route("api/inventory-items")]
public class InventoryItemController : BaseApiController
{
    private readonly IInventoryItemService _service;
    private readonly ILogger<InventoryItemController> _logger;

    public InventoryItemController(IInventoryItemService service, ILogger<InventoryItemController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET /api/inventory-items?shopId=1&brandId=2&search=screen
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? shopId,
        [FromQuery] int? brandId,
        [FromQuery] int? deviceCategoryId,
        [FromQuery] int? deviceModelId,
        [FromQuery] int? inventoryCategoryId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? lowStockOnly)
    {
        try
        {
            var resolvedShopId = shopId ?? GetCurrentShopId();
            if (resolvedShopId == 0)
                return UnauthorizedResponse("No shop selected");

            var filter = new InventoryItemFilterRequest
            {
                ShopId = resolvedShopId,
                BrandId = brandId,
                DeviceCategoryId = deviceCategoryId,
                DeviceModelId = deviceModelId,
                InventoryCategoryId = inventoryCategoryId,
                SearchTerm = search,
                IsActive = isActive,
                LowStockOnly = lowStockOnly
            };

            var items = await _service.GetInventoryItemsAsync(filter);
            return SuccessResponse(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory items");
            return ErrorResponse("Error retrieving inventory items", 500);
        }
    }

    // GET /api/inventory-items/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var item = await _service.GetInventoryItemByIdAsync(id, shopId);
            if (item == null)
                return NotFoundResponse("Inventory item not found");
            return SuccessResponse(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory item {Id}", id);
            return ErrorResponse("Error retrieving inventory item", 500);
        }
    }

    // POST /api/inventory-items
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInventoryItemRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            request.ShopId = shopId;

            // Check duplicate SKU
            if (!string.IsNullOrWhiteSpace(request.SKU))
            {
                var exists = await _service.SkuExistsAsync(request.SKU, shopId);
                if (exists)
                    return ErrorResponse("SKU already exists in this shop", 400);
            }

            var item = await _service.CreateInventoryItemAsync(request);
            _logger.LogInformation("InventoryItem created: {Id}", item.Id);

            return StatusCode(201, new
            {
                success = true,
                message = "Inventory item created successfully",
                data = item,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory item");
            return ErrorResponse("Error creating inventory item", 500);
        }
    }

    // PUT /api/inventory-items/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInventoryItemRequest request)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            // Check duplicate SKU (excluding this item)
            if (!string.IsNullOrWhiteSpace(request.SKU))
            {
                var exists = await _service.SkuExistsAsync(request.SKU, shopId, id);
                if (exists)
                    return ErrorResponse("SKU already exists in this shop", 400);
            }

            var item = await _service.UpdateInventoryItemAsync(id, request, shopId);
            if (item == null)
                return NotFoundResponse("Inventory item not found");

            _logger.LogInformation("InventoryItem updated: {Id}", id);
            return SuccessResponse(item, "Inventory item updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item {Id}", id);
            return ErrorResponse("Error updating inventory item", 500);
        }
    }

    // DELETE /api/inventory-items/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var deleted = await _service.DeleteInventoryItemAsync(id, shopId);
            if (!deleted)
                return NotFoundResponse("Inventory item not found");

            _logger.LogInformation("InventoryItem deleted: {Id}", id);
            return SuccessResponse((object?)null, "Inventory item deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item {Id}", id);
            return ErrorResponse("Error deleting inventory item", 500);
        }
    }

    // GET /api/inventory-items/generate-sku?brandId=1&categoryId=2&modelId=3
    [HttpGet("generate-sku")]
    public async Task<IActionResult> GenerateSku(
        [FromQuery] int brandId,
        [FromQuery] int categoryId,
        [FromQuery] int modelId)
    {
        try
        {
            var sku = await _service.GenerateSkuAsync(brandId, categoryId, modelId);
            return SuccessResponse(sku);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SKU");
            return ErrorResponse("Error generating SKU", 500);
        }
    }

    // GET /api/inventory-items/sku-exists?sku=ABC-DEF-GHI-0001&shopId=1
    [HttpGet("sku-exists")]
    public async Task<IActionResult> SkuExists(
        [FromQuery] string sku,
        [FromQuery] int shopId,
        [FromQuery] int? excludeItemId = null)
    {
        try
        {
            var exists = await _service.SkuExistsAsync(sku, shopId, excludeItemId);
            return SuccessResponse(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SKU");
            return ErrorResponse("Error checking SKU", 500);
        }
    }
}

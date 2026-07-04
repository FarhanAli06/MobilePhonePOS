using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Empire.Web.Authorization;
using Empire.Web.Services.InventoryManagement;
using Empire.Web.Services.Session;
using Empire.Web.DTOs.Inventory;
using Empire.Web.Services;
namespace Empire.Web.Controllers;

[SessionAuthorize]
[Route("Inventories")]
public class InventoriesController : BaseController
{
    private readonly IInventoryManagementApiService _inventoryService;
    private readonly ISessionHelper _sessionHelper;
    private readonly ILogger<InventoriesController> _logger;

    public InventoriesController(
        IInventoryManagementApiService inventoryService,
        ISessionHelper sessionHelper,
        ILogger<InventoriesController> logger,
        ITimezoneService timezoneService) : base(logger, timezoneService)
    {
        _inventoryService = inventoryService;
        _sessionHelper = sessionHelper;
        _logger = logger;
    }

    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        try
        {
            // Check authentication first
            if (!_sessionHelper.IsAuthenticated())
            {
                return RedirectToAction("Login", "Home");
            }

            var currentShopId = GetCurrentShopId();            ViewBag.PageTitle = "Inventory Management";
            
            return View();
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error loading inventory page: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet("GetInventoryItems")]
    public async Task<IActionResult> GetInventoryItems(
        int? brandId = null,
        int? deviceCategoryId = null,
        int? deviceModelId = null,
        int? inventoryCategoryId = null,
        string stockStatus = null)
    {
        try
        {
            var currentShopId = GetCurrentShopId();

            var items = await _inventoryService.GetInventoryItemsAsync(
                currentShopId,
                brandId,
                deviceCategoryId,
                deviceModelId,
                inventoryCategoryId,
                stockStatus);

            _logger.LogInformation("Found {Count} inventory items for shop {ShopId}", items.Count, currentShopId);

            // Use service to map items to response format
            var result = _inventoryService.MapInventoryItemsForResponse(items);

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetInventoryItems");
            return Json(new { success = false, message = $"Error loading inventory items: {ex.Message}" });
        }
    }

    [HttpGet("GetInventoryItem/{id:int}")]
    public async Task<IActionResult> GetInventoryItem(int id)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var item = await _inventoryService.GetInventoryItemByIdAsync(id, currentShopId);

            if (item == null)
            {
                return Json(new { success = false, message = "Inventory item not found" });
            }

            var result = new
            {
                id = item.Id,
                name = item.Name,
                sku = item.SKU,
                description = item.Description,
                brandId = item.BrandId,
                brandName = item.Brand,
                deviceCategoryId = item.DeviceCategoryId,
                deviceCategoryName = item.DeviceCategory,
                deviceModelId = item.DeviceModelId,
                deviceModelName = item.DeviceModel,
                inventoryCategoryId = item.InventoryCategoryId,
                inventoryCategoryName = item.InventoryCategory,
                itemId = item.ItemId,
                itemName = item.ItemName,
                categoryId = item.CategoryId,
                categoryName = item.CategoryName,
                currentStock = item.CurrentStock,
                reorderPoint = item.ReorderPoint,
                costPrice = item.CostPrice,
                retailPrice = item.RetailPrice,
                wholesalePrice = item.WholesalePrice,
                enableLowStockNotifications = item.EnableLowStockNotifications,
                isActive = item.IsActive,
                notes = item.Notes
            };

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error loading inventory item: {ex.Message}" });
        }
    }

    [HttpPost("CreateInventoryItem")]
    public async Task<IActionResult> CreateInventoryItem([FromBody] CreateInventoryItemRequest request)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                return Json(new { success = false, message = "No shop selected" });
            }

            // Validate required fields - CategoryId is the item type from the Categories table
            if (!(request.CategoryId > 0) && !(request.ItemId > 0))
            {
                return Json(new { success = false, message = "Item type is required" });
            }

            if (request.BrandId <= 0 || request.DeviceCategoryId <= 0 || 
                request.DeviceModelId <= 0 || request.InventoryCategoryId <= 0)
            {
                return Json(new { success = false, message = "All dropdown selections are required" });
            }

            // Generate SKU if not provided
            if (string.IsNullOrWhiteSpace(request.SKU))
            {
                request.SKU = await _inventoryService.GenerateSkuAsync(
                    request.BrandId, 
                    request.DeviceCategoryId, 
                    request.DeviceModelId);
            }

            // Check for duplicate SKU
            var skuExists = await _inventoryService.SkuExistsAsync(request.SKU, currentShopId);
            
            if (skuExists)
            {
                return Json(new { success = false, message = "SKU already exists" });
            }

            var inventoryItem = new InventoryItemDto
            {
                ShopId = currentShopId,
                ItemId = request.ItemId,
                CategoryId = request.CategoryId,
                Name = request.Name?.Trim() ?? string.Empty,
                SKU = request.SKU.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                BrandId = request.BrandId,
                DeviceCategoryId = request.DeviceCategoryId,
                DeviceModelId = request.DeviceModelId,
                InventoryCategoryId = request.InventoryCategoryId,
                CurrentStock = request.CurrentStock,
                ReorderPoint = request.ReorderPoint,
                CostPrice = request.CostPrice,
                RetailPrice = request.RetailPrice,
                WholesalePrice = request.WholesalePrice,
                EnableLowStockNotifications = request.EnableLowStockNotifications,
                IsActive = request.IsActive,
                Notes = request.Notes?.Trim() ?? string.Empty
            };

            var currentUserId = GetCurrentUserId();
            await _inventoryService.CreateInventoryItemAsync(inventoryItem, currentUserId);

            var stockMessage = request.CurrentStock > 0 
                ? $"Inventory item created with initial stock of {request.CurrentStock}."
                : "Inventory item created. Use Stock Movements to add stock.";
            
            return Json(new { success = true, message = stockMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory item");
            return Json(new { success = false, message = $"Error creating inventory item: {ex.Message}" });
        }
    }

    [HttpPut("UpdateInventoryItem/{id:int}")]
    public async Task<IActionResult> UpdateInventoryItem(int id, [FromBody] UpdateInventoryItemRequestDto request)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var item = await _inventoryService.GetInventoryItemByIdAsync(id, currentShopId);

            if (item == null)
            {
                return Json(new { success = false, message = "Inventory item not found" });
            }

            // Validate required fields - accept either CategoryId (new) or ItemId (legacy)
            if (!(request.CategoryId > 0) && !(request.ItemId > 0))
            {
                return Json(new { success = false, message = "Item type is required" });
            }

            // Check for duplicate SKU (excluding current item)
            if (!string.IsNullOrWhiteSpace(request.SKU))
            {
                var skuExists = await _inventoryService.SkuExistsAsync(request.SKU, currentShopId, id);
                
                if (skuExists)
                {
                    return Json(new { success = false, message = "SKU already exists" });
                }
            }

            // Update properties
            item.ItemId = request.ItemId;
            item.CategoryId = request.CategoryId;
            item.Name = request.Name?.Trim() ?? string.Empty;
            item.SKU = request.SKU?.Trim() ?? string.Empty;
            item.Description = request.Description?.Trim() ?? string.Empty;
            item.BrandId = request.BrandId ?? item.BrandId;
            item.DeviceCategoryId = request.DeviceCategoryId ?? item.DeviceCategoryId;
            item.DeviceModelId = request.DeviceModelId ?? item.DeviceModelId;
            item.InventoryCategoryId = request.InventoryCategoryId ?? item.InventoryCategoryId;
            item.ReorderPoint = request.ReorderPoint ?? item.ReorderPoint;
            item.CostPrice = request.CostPrice;
            item.RetailPrice = request.RetailPrice;
            item.WholesalePrice = request.WholesalePrice;
            item.EnableLowStockNotifications = request.EnableLowStockNotifications;
            item.IsActive = request.IsActive;
            item.Notes = request.Notes?.Trim() ?? string.Empty;

            await _inventoryService.UpdateInventoryItemAsync(item);

            return Json(new { success = true, message = "Inventory item updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item");
            return Json(new { success = false, message = $"Error updating inventory item: {ex.Message}" });
        }
    }

    [HttpGet("GenerateSku")]
    public async Task<IActionResult> GenerateSku([FromQuery] int brandId, [FromQuery] int categoryId, [FromQuery] int modelId)
    {
        try
        {
            if (brandId <= 0 || categoryId <= 0 || modelId <= 0)
                return Json(new { success = false, message = "All three IDs are required" });

            var sku = await _inventoryService.GenerateSkuAsync(brandId, categoryId, modelId);
            return Json(new { success = true, sku });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SKU preview");
            return Json(new { success = false, message = "Could not generate SKU" });
        }
    }

    [HttpDelete("DeleteInventoryItem/{id:int}")]
    public async Task<IActionResult> DeleteInventoryItem(int id)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var deleted = await _inventoryService.DeleteInventoryItemAsync(id, currentShopId);

            if (!deleted)
            {
                return Json(new { success = false, message = "Inventory item not found" });
            }

            return Json(new { success = true, message = "Inventory item deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item");
            return Json(new { success = false, message = $"Error deleting inventory item: {ex.Message}" });
        }
    }
}

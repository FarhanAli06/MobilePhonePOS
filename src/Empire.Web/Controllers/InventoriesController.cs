using Microsoft.AspNetCore.Mvc;
using Empire.Infrastructure.Data;
using Empire.Domain.Entities;
using Empire.Web.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Empire.Web.Controllers;

[Route("Inventories")]
public class InventoriesController : Controller
{
    private readonly EmpireDbContext _context;

    public InventoriesController(EmpireDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
    {
        try
        {
            // Check authentication first
            var isAuthenticated = HttpContext.Session.GetString("IsAuthenticated");
            var userId = HttpContext.Session.GetString("UserId");
            
            if (string.IsNullOrEmpty(isAuthenticated) || isAuthenticated != "true" || string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Home");
            }

            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                // Set a default shop ID of 1 for now to bypass the shop selection issue
                HttpContext.Session.SetInt32("CurrentShopId", 1);
                HttpContext.Session.SetString("CurrentShopName", "Default Shop");
                currentShopId = 1;
            }

            ViewBag.CurrentShopId = currentShopId;
            ViewBag.PageTitle = "Inventory Management";
            
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
            // For testing - bypass authentication temporarily
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                // Set default shop ID for testing
                currentShopId = 1;
            }

            var query = _context.InventoryItems
                .Include(i => i.Brand)
                .Include(i => i.DeviceCategory)
                .Include(i => i.DeviceModel)
                .Include(i => i.InventoryCategory)
                .Where(i => i.ShopId == currentShopId);

            // Apply filters
            if (brandId.HasValue)
                query = query.Where(i => i.BrandId == brandId.Value);

            if (deviceCategoryId.HasValue)
                query = query.Where(i => i.DeviceCategoryId == deviceCategoryId.Value);

            if (deviceModelId.HasValue)
                query = query.Where(i => i.DeviceModelId == deviceModelId.Value);

            if (inventoryCategoryId.HasValue)
                query = query.Where(i => i.InventoryCategoryId == inventoryCategoryId.Value);

            var items = await query.OrderBy(i => i.Name).ToListAsync();

            // Debug logging
            Console.WriteLine($"Found {items.Count} inventory items for shop {currentShopId}");

            // Apply stock status filter
            if (!string.IsNullOrEmpty(stockStatus))
            {
                items = stockStatus switch
                {
                    "out_of_stock" => items.Where(i => i.CurrentStock <= 0).ToList(),
                    "low_stock" => items.Where(i => i.CurrentStock > 0 && i.CurrentStock <= i.ReorderPoint).ToList(),
                    "in_stock" => items.Where(i => i.CurrentStock > i.ReorderPoint).ToList(),
                    _ => items
                };
            }

            var result = items.Select(i => new
            {
                id = i.Id,
                name = i.Name,
                sku = i.SKU,
                description = i.Description,
                brandId = i.BrandId,
                brandName = i.Brand?.Name ?? "Unknown",
                deviceCategoryId = i.DeviceCategoryId,
                deviceCategoryName = i.DeviceCategory?.Name ?? "Unknown",
                deviceModelId = i.DeviceModelId,
                deviceModelName = i.DeviceModel?.Name ?? "Unknown",
                inventoryCategoryId = i.InventoryCategoryId,
                inventoryCategoryName = i.InventoryCategory?.Name ?? "Unknown",
                currentStock = i.CurrentStock,
                reorderPoint = i.ReorderPoint,
                costPrice = i.CostPrice,
                retailPrice = i.RetailPrice,
                wholesalePrice = i.WholesalePrice,
                enableLowStockNotifications = i.EnableLowStockNotifications,
                isActive = i.IsActive,
                notes = i.Notes,
                createdDate = i.CreatedDate
            }).ToList();

            Console.WriteLine($"Returning {result.Count} items in JSON response");
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetInventoryItems: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Json(new { success = false, message = $"Error loading inventory items: {ex.Message}" });
        }
    }

    [HttpGet("GetInventoryItem/{id:int}")]
    public async Task<IActionResult> GetInventoryItem(int id)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var item = await _context.InventoryItems
                .Include(i => i.Brand)
                .Include(i => i.DeviceCategory)
                .Include(i => i.DeviceModel)
                .Include(i => i.InventoryCategory)
                .FirstOrDefaultAsync(i => i.Id == id && i.ShopId == currentShopId);

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
                brandName = item.Brand?.Name,
                deviceCategoryId = item.DeviceCategoryId,
                deviceCategoryName = item.DeviceCategory?.Name,
                deviceModelId = item.DeviceModelId,
                deviceModelName = item.DeviceModel?.Name,
                inventoryCategoryId = item.InventoryCategoryId,
                inventoryCategoryName = item.InventoryCategory?.Name,
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

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Json(new { success = false, message = "Item name is required" });
            }

            if (request.BrandId <= 0 || request.DeviceCategoryId <= 0 || 
                request.DeviceModelId <= 0 || request.InventoryCategoryId <= 0)
            {
                return Json(new { success = false, message = "All dropdown selections are required" });
            }

            // Generate SKU if not provided
            if (string.IsNullOrWhiteSpace(request.SKU))
            {
                request.SKU = await GenerateSkuAsync(request.BrandId, request.DeviceCategoryId, request.DeviceModelId);
            }

            // Check for duplicate SKU
            var existingSku = await _context.InventoryItems
                .AnyAsync(i => i.SKU == request.SKU && i.ShopId == currentShopId);
            
            if (existingSku)
            {
                return Json(new { success = false, message = "SKU already exists" });
            }

            var inventoryItem = new InventoryItem
            {
                ShopId = currentShopId,
                Name = request.Name.Trim(),
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
                Notes = request.Notes?.Trim() ?? string.Empty,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.InventoryItems.Add(inventoryItem);
            await _context.SaveChangesAsync();

            // Record initial stock if any
            if (request.CurrentStock > 0)
            {
                await RecordStockMovementInternal(inventoryItem.Id, "IN", request.CurrentStock, 
                    "Initial stock", "INITIAL", request.CostPrice);
            }

            return Json(new { success = true, message = "Inventory item created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error creating inventory item: {ex.Message}" });
        }
    }

    [HttpPut("UpdateInventoryItem/{id:int}")]
    public async Task<IActionResult> UpdateInventoryItem(int id, [FromBody] UpdateInventoryItemRequest request)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.ShopId == currentShopId);

            if (item == null)
            {
                return Json(new { success = false, message = "Inventory item not found" });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Json(new { success = false, message = "Item name is required" });
            }

            // Check for duplicate SKU (excluding current item)
            if (!string.IsNullOrWhiteSpace(request.SKU))
            {
                var existingSku = await _context.InventoryItems
                    .AnyAsync(i => i.SKU == request.SKU && i.ShopId == currentShopId && i.Id != id);
                
                if (existingSku)
                {
                    return Json(new { success = false, message = "SKU already exists" });
                }
            }

            // Update properties
            item.Name = request.Name.Trim();
            item.SKU = request.SKU?.Trim() ?? string.Empty;
            item.Description = request.Description?.Trim() ?? string.Empty;
            item.BrandId = request.BrandId;
            item.DeviceCategoryId = request.DeviceCategoryId;
            item.DeviceModelId = request.DeviceModelId;
            item.InventoryCategoryId = request.InventoryCategoryId;
            item.ReorderPoint = request.ReorderPoint;
            item.CostPrice = request.CostPrice;
            item.RetailPrice = request.RetailPrice;
            item.WholesalePrice = request.WholesalePrice;
            item.EnableLowStockNotifications = request.EnableLowStockNotifications;
            item.IsActive = request.IsActive;
            item.Notes = request.Notes?.Trim() ?? string.Empty;
            item.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Inventory item updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error updating inventory item: {ex.Message}" });
        }
    }

    [HttpDelete("DeleteInventoryItem/{id:int}")]
    public async Task<IActionResult> DeleteInventoryItem(int id)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == id && i.ShopId == currentShopId);

            if (item == null)
            {
                return Json(new { success = false, message = "Inventory item not found" });
            }

            // Check if item has stock movements
            var hasMovements = await _context.StockMovements
                .AnyAsync(sm => sm.InventoryItemId == id);

            if (hasMovements)
            {
                // Soft delete by marking as inactive
                item.IsActive = false;
                item.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Inventory item deactivated (has stock movement history)" });
            }
            else
            {
                // Hard delete if no movements
                _context.InventoryItems.Remove(item);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Inventory item deleted successfully" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error deleting inventory item: {ex.Message}" });
        }
    }

    [HttpPost("RecordStockMovement")]
    public async Task<IActionResult> RecordStockMovement([FromBody] StockMovementRequest request)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId && i.ShopId == currentShopId);

            if (item == null)
            {
                return Json(new { success = false, message = "Inventory item not found" });
            }

            await RecordStockMovementInternal(
                request.InventoryItemId,
                request.MovementType,
                request.Quantity,
                request.Reason,
                request.ReferenceNumber,
                request.UnitCost
            );

            return Json(new { success = true, message = "Stock movement recorded successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error recording stock movement: {ex.Message}" });
        }
    }

    private async Task RecordStockMovementInternal(int inventoryItemId, string movementType, int quantity, 
        string reason, string referenceNumber, decimal unitCost)
    {
        var item = await _context.InventoryItems.FindAsync(inventoryItemId);
        if (item == null) return;

        // Calculate new stock level
        var newStock = movementType switch
        {
            "IN" => item.CurrentStock + quantity,
            "OUT" => Math.Max(0, item.CurrentStock - quantity),
            "ADJUSTMENT" => quantity, // Direct adjustment to specific quantity
            "TRANSFER" => Math.Max(0, item.CurrentStock - quantity),
            _ => item.CurrentStock
        };

        // Get current user ID, default to 1 if not found (assuming admin user exists)
        var currentUserId = GetCurrentUserId();
        if (currentUserId <= 0)
        {
            // Try to get the first user or default to user ID 1
            var firstUser = await _context.Users.FirstOrDefaultAsync();
            currentUserId = firstUser?.Id ?? 1; // Default to user ID 1
        }

        // Create stock movement record
        var movement = new StockMovement
        {
            InventoryItemId = inventoryItemId,
            MovementType = movementType,
            Quantity = quantity,
            PreviousStock = item.CurrentStock,
            NewStock = newStock,
            UnitCost = unitCost,
            TotalCost = unitCost * quantity,
            Reason = reason ?? string.Empty,
            ReferenceNumber = referenceNumber ?? string.Empty,
            MovementDate = DateTime.UtcNow,
            CreatedBy = currentUserId,
            CreatedDate = DateTime.UtcNow
        };

        _context.StockMovements.Add(movement);

        // Update item stock
        item.CurrentStock = newStock;
        item.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task<string> GenerateSkuAsync(int brandId, int deviceCategoryId, int deviceModelId)
    {
        var brand = await _context.Brands.FindAsync(brandId);
        var category = await _context.DeviceCategories.FindAsync(deviceCategoryId);
        var model = await _context.DeviceModels.FindAsync(deviceModelId);

        var brandCode = brand?.Name?.Substring(0, Math.Min(3, brand.Name.Length)).ToUpper() ?? "UNK";
        var categoryCode = category?.Name?.Substring(0, Math.Min(2, category.Name.Length)).ToUpper() ?? "UN";
        var modelCode = model?.Name?.Substring(0, Math.Min(4, model.Name.Length)).ToUpper() ?? "UNKN";

        // Remove spaces and special characters
        brandCode = new string(brandCode.Where(char.IsLetterOrDigit).ToArray());
        categoryCode = new string(categoryCode.Where(char.IsLetterOrDigit).ToArray());
        modelCode = new string(modelCode.Where(char.IsLetterOrDigit).ToArray());

        var baseSku = $"{brandCode}-{categoryCode}-{modelCode}";
        
        // Check for duplicates and add suffix if needed
        var counter = 1;
        var sku = baseSku;
        
        while (await _context.InventoryItems.AnyAsync(i => i.SKU == sku))
        {
            sku = $"{baseSku}-{counter:D2}";
            counter++;
        }

        return sku;
    }

    private int GetCurrentShopId()
    {
        return HttpContext.Session.GetInt32("CurrentShopId") ?? 0;
    }

    private int GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId") ?? 0;
    }
}

// Request models
public class CreateInventoryItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public int DeviceCategoryId { get; set; }
    public int DeviceModelId { get; set; }
    public int InventoryCategoryId { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderPoint { get; set; } = 5;
    public decimal CostPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public bool EnableLowStockNotifications { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}

public class UpdateInventoryItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public int DeviceCategoryId { get; set; }
    public int DeviceModelId { get; set; }
    public int InventoryCategoryId { get; set; }
    public int ReorderPoint { get; set; } = 5;
    public decimal CostPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public bool EnableLowStockNotifications { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}

public class StockMovementRequest
{
    public int InventoryItemId { get; set; }
    public string MovementType { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT, TRANSFER
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
}


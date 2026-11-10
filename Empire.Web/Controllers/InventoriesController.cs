using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Empire.Web.Authorization;
using Empire.Web.Services.InventoryManagement;
using Empire.Web.Services.Session;
using Empire.Web.DTOs.Inventory;
using Empire.Web.Services;
using System.Text;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
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

            var currentShopId = GetCurrentShopId();
            ViewBag.PageTitle = "Inventory Management";
            ViewBag.IsInventoryAdmin = IsAdmin();
            
            return View();
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error loading inventory page: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost("ExtractSupplierDocument")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> ExtractSupplierDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Choose a PDF or screenshot first." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".png", ".jpg", ".jpeg", ".webp"
        };

        if (!allowedExtensions.Contains(extension))
        {
            return Json(new { success = false, message = "Only PDF, PNG, JPG, JPEG, and WEBP invoice files are supported." });
        }

        try
        {
            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (ocrEngine == null)
            {
                return Json(new { success = false, message = "Windows text recognition is unavailable. Install the English language OCR feature, then try again." });
            }

            await using var uploaded = file.OpenReadStream();
            using var content = new MemoryStream();
            await uploaded.CopyToAsync(content);
            content.Position = 0;

            var text = extension == ".pdf"
                ? await ReadPdfTextAsync(content, ocrEngine)
                : await ReadImageTextAsync(content, ocrEngine);

            return Json(new
            {
                success = true,
                text = text.Trim(),
                message = string.IsNullOrWhiteSpace(text)
                    ? "No readable text was found in the document."
                    : "Document text extracted successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to extract supplier document {FileName}", file.FileName);
            return Json(new { success = false, message = "The document could not be read. Try a clearer screenshot or paste the order text instead." });
        }
    }

    private static async Task<string> ReadImageTextAsync(Stream source, OcrEngine ocrEngine)
    {
        using var randomAccessStream = source.AsRandomAccessStream();
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var result = await ocrEngine.RecognizeAsync(bitmap);
        return FormatOcrRows(result);
    }

    private static async Task<string> ReadPdfTextAsync(Stream source, OcrEngine ocrEngine)
    {
        using var randomAccessStream = source.AsRandomAccessStream();
        var document = await PdfDocument.LoadFromStreamAsync(randomAccessStream);
        var text = new StringBuilder();
        var pageCount = Math.Min(document.PageCount, 25u);

        for (uint pageNumber = 0; pageNumber < pageCount; pageNumber++)
        {
            using var page = document.GetPage(pageNumber);
            using var renderedPage = new InMemoryRandomAccessStream();
            var destinationWidth = (uint)Math.Clamp(page.Size.Width * 2.0, 1200.0, 2400.0);
            await page.RenderToStreamAsync(renderedPage, new PdfPageRenderOptions { DestinationWidth = destinationWidth });
            renderedPage.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(renderedPage);
            using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var result = await ocrEngine.RecognizeAsync(bitmap);
            var pageText = FormatOcrRows(result);
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                text.AppendLine(pageText);
            }
        }

        return text.ToString();
    }

    private static string FormatOcrRows(OcrResult result)
    {
        var segments = result.Lines
            .Where(line => line.Words.Count > 0 && !string.IsNullOrWhiteSpace(line.Text))
            .Select(line => new
            {
                Top = line.Words.Min(word => word.BoundingRect.Y),
                Left = line.Words.Min(word => word.BoundingRect.X),
                Text = line.Text.Trim()
            })
            .OrderBy(segment => segment.Top)
            .ThenBy(segment => segment.Left)
            .ToList();

        var rows = new List<List<(double Top, double Left, string Text)>>();
        foreach (var segment in segments)
        {
            var row = rows.LastOrDefault();
            var rowTop = row?.Average(value => value.Top) ?? double.MinValue;
            if (row == null || Math.Abs(segment.Top - rowTop) > 26)
            {
                row = new List<(double Top, double Left, string Text)>();
                rows.Add(row);
            }
            row.Add((segment.Top, segment.Left, segment.Text));
        }

        return string.Join(Environment.NewLine, rows.Select(row =>
            string.Join("   ", row.OrderBy(value => value.Left).Select(value => value.Text))));
    }

    [HttpGet("GetInventoryItems")]
    public async Task<IActionResult> GetInventoryItems(
        int? brandId = null,
        int? deviceCategoryId = null,
        int? deviceModelId = null,
        int? inventoryCategoryId = null,
        string stockStatus = null,
        string search = null)
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
                stockStatus,
                search);

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
                variant = item.Variant,
                color = item.Color,
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
                Variant = request.Variant?.Trim() ?? string.Empty,
                Color = request.Color?.Trim() ?? string.Empty,
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
            var createdItem = await _inventoryService.CreateInventoryItemAsync(inventoryItem, currentUserId);

            var stockMessage = request.CurrentStock > 0 
                ? $"Inventory item created with initial stock of {request.CurrentStock}."
                : "Inventory item created. Use Stock Movements to add stock.";
            
            return Json(new { success = true, message = stockMessage, id = createdItem.Id });
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
            item.Variant = request.Variant?.Trim() ?? string.Empty;
            item.Color = request.Color?.Trim() ?? string.Empty;
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
            if (!IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "Only an administrator can delete inventory." });
            }

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

    [HttpPost("ReceiveBatch/{id:int}")]
    public async Task<IActionResult> ReceiveBatch(int id, [FromBody] ReceiveInventoryBatchRequest request)
    {
        try
        {
            if (request.Quantity <= 0) return Json(new { success = false, message = "Quantity must be greater than zero." });
            var batch = await _inventoryService.ReceiveBatchAsync(id, request);
            return batch == null
                ? Json(new { success = false, message = "The batch could not be received." })
                : Json(new { success = true, message = $"Received {request.Quantity} item(s) as a new FIFO batch.", data = batch });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving FIFO batch for inventory item {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("BatchHistory/{id:int}")]
    public async Task<IActionResult> BatchHistory(int id)
    {
        try { return Json(new { success = true, data = await _inventoryService.GetBatchesAsync(id) }); }
        catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
    }
}

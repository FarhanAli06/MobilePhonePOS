using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Empire.Web.Services.InventoryTransaction;
using Empire.Web.DTOs.InventoryTransaction;
using Empire.Web.Services;

namespace Empire.Web.Controllers;

public class InventoryTransactionsController : BaseController
{
    private readonly IInventoryTransactionApiService _transactionService;
    private readonly ILogger<InventoryTransactionsController> _logger;

    public InventoryTransactionsController(IInventoryTransactionApiService transactionService,
        ILogger<InventoryTransactionsController> logger,
        ITimezoneService timezoneService) : base(logger, timezoneService)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Display all inventory transactions with filters
    /// </summary>
    public async Task<IActionResult> Index(
        string? transactionType = null,
        int? inventoryItemId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            
            if (currentShopId == 0)
            {
                ViewBag.InventoryItems = new List<object>();
                TempData["Error"] = "Invalid shop session. Please log out and log in again.";
                return View(new List<InventoryTransactionDto>());
            }

            var transactions = await _transactionService.GetTransactionsAsync(
                currentShopId,
                transactionType,
                inventoryItemId,
                startDate,
                endDate);

            // Get inventory items for filter dropdown
            var inventoryItems = await _transactionService.GetInventoryItemsByShopAsync(currentShopId);
            ViewBag.InventoryItems = inventoryItems.Select(i => new { i.Id, i.Name }).ToList();

            ViewBag.TransactionType = transactionType;
            ViewBag.InventoryItemId = inventoryItemId;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inventory transactions");
            TempData["Error"] = "Error loading transactions: " + ex.Message;
            return View(new List<InventoryTransactionDto>());
        }
    }

    /// <summary>
    /// Display transaction history for a specific inventory item
    /// </summary>
    public async Task<IActionResult> ItemHistory(int id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Dashboard", "Home");

        try
        {
            var currentShopId = GetCurrentShopId();

            var item = await _transactionService.GetInventoryItemWithDetailsAsync(id, currentShopId);

            if (item == null)
            {
                TempData["Error"] = "Inventory item not found";
                return RedirectToAction("Index");
            }

            var transactions = await _transactionService.GetItemHistoryAsync(id);

            ViewBag.Item = item;

            return View(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading item history for item {ItemId}", id);
            TempData["Error"] = "Error loading item history: " + ex.Message;
            return RedirectToAction("Index");
        }
    }

    /// <summary>
    /// Create a new stock IN transaction (receiving stock)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AddStock()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Dashboard", "Home");

        var currentShopId = GetCurrentShopId();

        var inventoryItems = await _transactionService.GetInventoryItemsByShopAsync(currentShopId);
        ViewBag.InventoryItems = inventoryItems.Select(i => new { i.Id, i.Name }).ToList();

        return View();
    }

    /// <summary>
    /// Create a new stock IN transaction (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStock(
        int inventoryItemId,
        int quantity,
        decimal? unitCost,
        string reason,
        string? referenceNumber,
        string? notes)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Dashboard", "Home");

        try
        {
            var currentShopId = GetCurrentShopId();
            var currentUserId = GetCurrentUserId();
            
            _logger.LogInformation(
                "AddStock called - ShopId: {ShopId}, UserId: {UserId}, ItemId: {ItemId}, Qty: {Qty}",
                currentShopId, currentUserId, inventoryItemId, quantity);
            
            if (currentShopId == 0)
            {
                _logger.LogError("AddStock failed - Invalid shop session");
                TempData["Error"] = "Invalid shop session. Please log in again.";
                return RedirectToAction("Dashboard", "Home");
            }
            
            if (currentUserId == 0)
            {
                _logger.LogError("AddStock failed - Invalid user session");
                TempData["Error"] = "Invalid user session. Please log in again.";
                return RedirectToAction("Dashboard", "Home");
            }

            // Verify item belongs to current shop
            var item = await _transactionService.GetInventoryItemWithDetailsAsync(inventoryItemId, currentShopId);

            if (item == null)
            {
                TempData["Error"] = "Inventory item not found";
                return RedirectToAction("AddStock");
            }

            // Create IN transaction
            await _transactionService.CreateStockInTransactionAsync(
                inventoryItemId,
                quantity,
                reason,
                currentUserId,
                "PURCHASE",
                null,
                referenceNumber,
                unitCost,
                notes
            );

            TempData["Success"] = $"Stock added successfully! {quantity} units of {item.Name} added.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding stock");
            TempData["Error"] = "Error adding stock: " + ex.Message;
            return RedirectToAction("AddStock");
        }
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Session.GetInt32("UserId").HasValue;
    }

    [HttpGet]
    public IActionResult TestSession()
    {
        var sessionData = new
        {
            userId = HttpContext.Session.GetString("UserId"),
            currentShopId = HttpContext.Session.GetString("CurrentShopId"),
            currentShopName = HttpContext.Session.GetString("CurrentShopName"),
            isAuthenticated = HttpContext.Session.GetString("IsAuthenticated"),
            userName = HttpContext.Session.GetString("UserName"),
            sessionId = HttpContext.Session.Id,
            sessionAvailable = HttpContext.Session.IsAvailable
        };
        
        _logger.LogInformation("TestSession called: {@SessionData}", sessionData);
        
        return Json(new { success = true, session = sessionData });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetInventoryItems()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            
            _logger.LogInformation("GetInventoryItems called. ShopId: {ShopId}", currentShopId);
            
            if (currentShopId == 0)
            {
                _logger.LogWarning("GetInventoryItems: Invalid shop session (shopId = 0)");
                return Json(new { success = false, message = "Invalid shop session. Please log in again." });
            }
            
            var items = await _transactionService.GetInventoryItemsByShopAsync(currentShopId);
            
            var itemsData = items.Select(i => new
            {
                id = i.Id,
                name = i.Name,
                stock = i.CurrentStock,
                isActive = i.IsActive
            }).ToList();
            
            _logger.LogInformation("GetInventoryItems: Returning {Count} items for shop {ShopId}", itemsData.Count, currentShopId);

            return Json(new { success = true, data = itemsData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory items");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions(string? type, int? itemId, string? dateFrom, string? dateTo)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
                startDate = fromDate;

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
                endDate = toDate;

            var transactions = await _transactionService.GetTransactionsAsync(
                currentShopId,
                type,
                itemId,
                startDate,
                endDate);

            var transactionsData = transactions.Select(t => new
            {
                transactionDate = t.TransactionDate,
                itemName = t.InventoryItem.Name,
                transactionType = t.TransactionType,
                quantity = t.Quantity,
                unitCost = t.UnitCost,
                totalCost = t.TotalCost,
                referenceNumber = t.ReferenceNumber,
                reason = t.Reason,
                createdBy = t.CreatedByUser != null ? t.CreatedByUser.Username : "System"
            }).ToList();

            return Json(new { success = true, data = transactionsData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AdjustStock(int inventoryItemId, string transactionType, int quantity, string reason, string? notes)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var currentUserId = GetCurrentUserId();

            var item = await _transactionService.GetInventoryItemWithDetailsAsync(inventoryItemId, currentShopId);

            if (item == null)
                return Json(new { success = false, message = "Inventory item not found" });

            if (transactionType == "IN")
            {
                await _transactionService.CreateStockInTransactionAsync(
                    inventoryItemId,
                    quantity,
                    reason,
                    currentUserId,
                    "ADJUSTMENT",
                    null,
                    null,
                    item.CostPrice,
                    notes
                );
            }
            else if (transactionType == "OUT")
            {
                await _transactionService.CreateStockOutTransactionAsync(
                    inventoryItemId,
                    quantity,
                    reason,
                    currentUserId,
                    "ADJUSTMENT",
                    null,
                    null,
                    notes
                );
            }
            else
            {
                return Json(new { success = false, message = "Invalid transaction type" });
            }

            return Json(new { success = true, message = "Stock adjusted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock");
            return Json(new { success = false, message = ex.Message });
        }
    }
}

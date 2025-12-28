using Microsoft.AspNetCore.Mvc;
using Empire.Infrastructure.Data;
using Empire.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Empire.Web.Controllers;

public class InventoryTransactionsController : Controller
{
    private readonly EmpireDbContext _context;
    private readonly InventoryTransactionService _transactionService;
    private readonly ILogger<InventoryTransactionsController> _logger;

    public InventoryTransactionsController(
        EmpireDbContext context,
        InventoryTransactionService transactionService,
        ILogger<InventoryTransactionsController> logger)
    {
        _context = context;
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
        // Temporarily disabled for debugging 302 redirect
        // if (!IsAuthenticated())
        //     return RedirectToAction("Dashboard", "Home");

        try
        {
            var currentShopId = GetCurrentShopId();
            
            // Fallback: Try to set shop session if missing
            if (currentShopId == 0)
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (int.TryParse(userIdString, out int userId) && userId > 0)
                {
                    // Try to get user's first shop
                    var userShop = await _context.UserShopRoles
                        .Include(sr => sr.Shop)
                        .Where(sr => sr.UserId == userId)
                        .FirstOrDefaultAsync();
                    
                    if (userShop != null)
                    {
                        HttpContext.Session.SetInt32("CurrentShopId", userShop.ShopId);
                        HttpContext.Session.SetString("CurrentShopName", userShop.Shop.Name);
                        currentShopId = userShop.ShopId;
                        _logger.LogInformation("Set CurrentShopId to {ShopId} for user {UserId}", currentShopId, userId);
                    }
                }
            }
            
            if (currentShopId == 0)
            {
                ViewBag.InventoryItems = new List<object>();
                TempData["Error"] = "Invalid shop session. Please log out and log in again.";
                return View(new List<Empire.Domain.Entities.InventoryTransaction>());
            }

            var query = _context.InventoryTransactions
                .Include(t => t.InventoryItem)
                    .ThenInclude(i => i.Brand)
                .Include(t => t.InventoryItem)
                    .ThenInclude(i => i.DeviceCategory)
                .Include(t => t.InventoryItem)
                    .ThenInclude(i => i.DeviceModel)
                .Include(t => t.CreatedByUser)
                .Where(t => t.InventoryItem.ShopId == currentShopId)
                .AsQueryable();

            // Filter by transaction type
            if (!string.IsNullOrEmpty(transactionType))
            {
                query = query.Where(t => t.TransactionType == transactionType);
            }

            // Filter by inventory item
            if (inventoryItemId.HasValue && inventoryItemId.Value > 0)
            {
                query = query.Where(t => t.InventoryItemId == inventoryItemId.Value);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= endDate.Value.AddDays(1));
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Take(500) // Limit to recent 500 transactions
                .ToListAsync();

            // Get inventory items for filter dropdown
            ViewBag.InventoryItems = await _context.InventoryItems
                .Where(i => i.ShopId == currentShopId && i.IsActive)
                .OrderBy(i => i.Name)
                .Select(i => new { i.Id, i.Name })
                .ToListAsync();

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
            return View(new List<Empire.Domain.Entities.InventoryTransaction>());
        }
    }

    /// <summary>
    /// Display stock details for all inventory items
    /// </summary>
    public async Task<IActionResult> StockLevels()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Dashboard", "Home");

        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                TempData["Error"] = "Invalid shop session. Please log in again.";
                return RedirectToAction("Dashboard", "Home");
            }
            var stockDetails = await _transactionService.GetAllStockDetailsAsync(currentShopId);

            return View(stockDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stock levels");
            TempData["Error"] = "Error loading stock levels: " + ex.Message;
            return View(new List<InventoryStockDetails>());
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

            var item = await _context.InventoryItems
                .Include(i => i.Brand)
                .Include(i => i.DeviceCategory)
                .Include(i => i.DeviceModel)
                .Include(i => i.InventoryCategory)
                .FirstOrDefaultAsync(i => i.Id == id && i.ShopId == currentShopId);

            if (item == null)
            {
                TempData["Error"] = "Inventory item not found";
                return RedirectToAction("Index");
            }

            var transactions = await _context.InventoryTransactions
                .Include(t => t.CreatedByUser)
                .Where(t => t.InventoryItemId == id)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var stockDetails = await _transactionService.GetStockDetailsAsync(id);

            ViewBag.Item = item;
            ViewBag.StockDetails = stockDetails;

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

        ViewBag.InventoryItems = await _context.InventoryItems
            .Where(i => i.ShopId == currentShopId && i.IsActive)
            .OrderBy(i => i.Name)
            .Select(i => new { i.Id, i.Name })
            .ToListAsync();

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
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.ShopId == currentShopId);

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
            return RedirectToAction("StockLevels");
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

    private int GetCurrentShopId()
    {
        var shopIdString = HttpContext.Session.GetString("CurrentShopId");
        var userId = HttpContext.Session.GetString("UserId");
        var isAuth = HttpContext.Session.GetString("IsAuthenticated");
        
        _logger.LogInformation(
            "GetCurrentShopId - ShopIdString: '{ShopId}', UserId: '{UserId}', IsAuth: '{IsAuth}'",
            shopIdString ?? "NULL", userId ?? "NULL", isAuth ?? "NULL");
        
        if (int.TryParse(shopIdString, out int shopId) && shopId > 0)
        {
            return shopId;
        }
        
        _logger.LogWarning("GetCurrentShopId returning 0 - invalid or missing shop session");
        return 0;
    }

    private int GetCurrentUserId()
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        return int.TryParse(userIdString, out int userId) ? userId : 0;
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
            
            // First, check total items for this shop (without IsActive filter)
            var totalItems = await _context.InventoryItems
                .Where(i => i.ShopId == currentShopId)
                .CountAsync();
            
            _logger.LogInformation("GetInventoryItems: Total items for shop {ShopId}: {Total}", currentShopId, totalItems);
            
            // Check how many are active
            var activeCount = await _context.InventoryItems
                .Where(i => i.ShopId == currentShopId && i.IsActive)
                .CountAsync();
            
            _logger.LogInformation("GetInventoryItems: Active items for shop {ShopId}: {Active}", currentShopId, activeCount);
            
            // Get the items (remove IsActive filter temporarily to see all items)
            var items = await _context.InventoryItems
                .Where(i => i.ShopId == currentShopId) // Removed IsActive filter
                .OrderBy(i => i.Name)
                .Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    stock = i.CurrentStock,
                    isActive = i.IsActive
                })
                .ToListAsync();
            
            _logger.LogInformation("GetInventoryItems: Returning {Count} items for shop {ShopId}", items.Count, currentShopId);

            return Json(new { success = true, data = items });
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
            var query = _context.InventoryTransactions
                .Include(t => t.InventoryItem)
                .Include(t => t.CreatedByUser)
                .Where(t => t.InventoryItem.ShopId == currentShopId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.TransactionType == type);

            if (itemId.HasValue && itemId.Value > 0)
                query = query.Where(t => t.InventoryItemId == itemId.Value);

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
                query = query.Where(t => t.TransactionDate >= fromDate);

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
                query = query.Where(t => t.TransactionDate <= toDate.AddDays(1));

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Take(200)
                .Select(t => new
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
                })
                .ToListAsync();

            return Json(new { success = true, data = transactions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions");
            return Json(new { success = false, message = ex.Message });
        }
    }

    // Removed duplicate AddStock method - use the one with [ValidateAntiForgeryToken] above

    [HttpPost]
    public async Task<IActionResult> AdjustStock(int inventoryItemId, string transactionType, int quantity, string reason, string? notes)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var currentUserId = GetCurrentUserId();

            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.ShopId == currentShopId);

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

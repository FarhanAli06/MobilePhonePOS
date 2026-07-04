using Empire.Domain.Entities;
using Empire.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Empire.Infrastructure.Services;

/// <summary>
/// Service for managing inventory transactions
/// Handles creation of IN and OUT transactions for stock movements
/// </summary>
public class InventoryTransactionService
{
    private readonly EmpireDbContext _context;
    private readonly ILogger<InventoryTransactionService> _logger;

    public InventoryTransactionService(
        EmpireDbContext context,
        ILogger<InventoryTransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a stock IN transaction (receiving stock)
    /// </summary>
    public async Task<InventoryTransaction> CreateStockInTransactionAsync(
        int inventoryItemId,
        int quantity,
        string reason,
        int createdByUserId,
        string? referenceType = null,
        int? referenceId = null,
        string? referenceNumber = null,
        decimal? unitCost = null,
        string? notes = null)
    {
        var transaction = new InventoryTransaction
        {
            InventoryItemId = inventoryItemId,
            TransactionDate = DateTime.UtcNow,
            TransactionType = "IN",
            Quantity = quantity,
            Reason = reason,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber,
            UnitCost = unitCost,
            TotalCost = unitCost.HasValue ? quantity * unitCost.Value : null,
            CreatedByUserId = createdByUserId,
            Notes = notes
        };

        // Update InventoryItem.CurrentStock to keep it in sync
        var inventoryItem = await _context.InventoryItems.FindAsync(inventoryItemId);
        if (inventoryItem != null)
        {
            inventoryItem.CurrentStock += quantity;
            inventoryItem.UpdatedDate = DateTime.UtcNow;
        }
        
        _context.InventoryTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Stock IN transaction created: Item {ItemId}, Qty {Quantity}, Reason: {Reason}",
            inventoryItemId, quantity, reason);

        return transaction;
    }

    /// <summary>
    /// Create a stock OUT transaction (using/selling stock)
    /// </summary>
    public async Task<InventoryTransaction> CreateStockOutTransactionAsync(
        int inventoryItemId,
        int quantity,
        string reason,
        int createdByUserId,
        string? referenceType = null,
        int? referenceId = null,
        string? referenceNumber = null,
        string? notes = null)
    {
        // Get inventory item and check stock
        var inventoryItem = await _context.InventoryItems.FindAsync(inventoryItemId);
        if (inventoryItem == null)
        {
            throw new InvalidOperationException($"Inventory item {inventoryItemId} not found");
        }
        
        // Check if enough stock is available
        if (inventoryItem.CurrentStock < quantity)
        {
            _logger.LogWarning(
                "Insufficient stock for item {ItemId}. Current: {Current}, Requested: {Requested}",
                inventoryItemId, inventoryItem.CurrentStock, quantity);
            
            throw new InvalidOperationException(
                $"Insufficient stock. Current stock: {inventoryItem.CurrentStock}, Requested: {quantity}");
        }

        // Update CurrentStock
        inventoryItem.CurrentStock -= quantity;
        inventoryItem.UpdatedDate = DateTime.UtcNow;
        
        var transaction = new InventoryTransaction
        {
            InventoryItemId = inventoryItemId,
            TransactionDate = DateTime.UtcNow,
            TransactionType = "OUT",
            Quantity = quantity,
            Reason = reason,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceNumber = referenceNumber,
            CreatedByUserId = createdByUserId,
            Notes = notes
        };

        _context.InventoryTransactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Stock OUT transaction created: Item {ItemId}, Qty {Quantity}, Reason: {Reason}",
            inventoryItemId, quantity, reason);

        return transaction;
    }

    /// <summary>
    /// Create stock OUT transaction for repair part usage
    /// </summary>
    public async Task<InventoryTransaction> CreateRepairPartTransactionAsync(
        int inventoryItemId,
        int quantity,
        int repairId,
        string repairNumber,
        int createdByUserId)
    {
        return await CreateStockOutTransactionAsync(
            inventoryItemId,
            quantity,
            $"Used in Repair #{repairNumber}",
            createdByUserId,
            "REPAIR",
            repairId,
            repairNumber,
            $"Part used in repair service");
    }

    /// <summary>
    /// Create stock OUT transaction for sale item
    /// </summary>
    public async Task<InventoryTransaction> CreateSaleItemTransactionAsync(
        int inventoryItemId,
        int quantity,
        int saleId,
        string saleNumber,
        int createdByUserId)
    {
        return await CreateStockOutTransactionAsync(
            inventoryItemId,
            quantity,
            $"Sold via POS - Sale #{saleNumber}",
            createdByUserId,
            "SALE",
            saleId,
            saleNumber,
            $"Item sold through point of sale");
    }

    /// <summary>
    /// Get current stock for an inventory item (calculated from transactions)
    /// </summary>
    public async Task<int> GetCurrentStockAsync(int inventoryItemId)
    {
        var totalIn = await _context.InventoryTransactions
            .Where(t => t.InventoryItemId == inventoryItemId && t.TransactionType == "IN")
            .SumAsync(t => (int?)t.Quantity) ?? 0;

        var totalOut = await _context.InventoryTransactions
            .Where(t => t.InventoryItemId == inventoryItemId && t.TransactionType == "OUT")
            .SumAsync(t => (int?)t.Quantity) ?? 0;

        return totalIn - totalOut;
    }

    /// <summary>
    /// Get stock details for an inventory item
    /// </summary>
    public async Task<InventoryStockDetails> GetStockDetailsAsync(int inventoryItemId)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Brand)
            .Include(i => i.DeviceCategory)
            .Include(i => i.DeviceModel)
            .Include(i => i.InventoryCategory)
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId);

        if (item == null)
        {
            throw new InvalidOperationException($"Inventory item {inventoryItemId} not found");
        }

        var totalIn = await _context.InventoryTransactions
            .Where(t => t.InventoryItemId == inventoryItemId && t.TransactionType == "IN")
            .SumAsync(t => (int?)t.Quantity) ?? 0;

        var totalOut = await _context.InventoryTransactions
            .Where(t => t.InventoryItemId == inventoryItemId && t.TransactionType == "OUT")
            .SumAsync(t => (int?)t.Quantity) ?? 0;

        var currentStock = totalIn - totalOut;

        return new InventoryStockDetails
        {
            InventoryItemId = inventoryItemId,
            ItemName = item.Name,
            SKU = item.SKU,
            BrandName = item.Brand?.Name ?? "",
            CategoryName = item.DeviceCategory?.Name ?? "",
            ModelName = item.DeviceModel?.Name ?? "",
            TotalIn = totalIn,
            TotalOut = totalOut,
            CurrentStock = currentStock,
            MinStockLevel = item.MinStockLevel,
            ReorderPoint = item.ReorderPoint,
            ReorderQuantity = item.ReorderQuantity,
            IsLowStock = currentStock <= item.MinStockLevel,
            IsOutOfStock = currentStock <= 0,
            StockStatus = currentStock <= 0 ? "Out of Stock" :
                         currentStock <= item.MinStockLevel ? "Low Stock" : "In Stock"
        };
    }

    /// <summary>
    /// Get stock details for all items in a shop
    /// </summary>
    public async Task<List<InventoryStockDetails>> GetAllStockDetailsAsync(int shopId)
    {
        var items = await _context.InventoryItems
            .Include(i => i.Brand)
            .Include(i => i.DeviceCategory)
            .Include(i => i.DeviceModel)
            .Include(i => i.InventoryCategory)
            .Where(i => i.ShopId == shopId && i.IsActive)
            .ToListAsync();

        var stockDetails = new List<InventoryStockDetails>();

        foreach (var item in items)
        {
            var totalIn = await _context.InventoryTransactions
                .Where(t => t.InventoryItemId == item.Id && t.TransactionType == "IN")
                .SumAsync(t => (int?)t.Quantity) ?? 0;

            var totalOut = await _context.InventoryTransactions
                .Where(t => t.InventoryItemId == item.Id && t.TransactionType == "OUT")
                .SumAsync(t => (int?)t.Quantity) ?? 0;

            var currentStock = totalIn - totalOut;

            stockDetails.Add(new InventoryStockDetails
            {
                InventoryItemId = item.Id,
                ItemName = item.Name,
                SKU = item.SKU,
                BrandName = item.Brand?.Name ?? "",
                CategoryName = item.DeviceCategory?.Name ?? "",
                ModelName = item.DeviceModel?.Name ?? "",
                TotalIn = totalIn,
                TotalOut = totalOut,
                CurrentStock = currentStock,
                MinStockLevel = item.MinStockLevel,
                ReorderPoint = item.ReorderPoint,
                ReorderQuantity = item.ReorderQuantity,
                CostPrice = item.CostPrice,
                RetailPrice = item.RetailPrice,
                IsLowStock = currentStock <= item.MinStockLevel,
                IsOutOfStock = currentStock <= 0,
                StockStatus = currentStock <= 0 ? "Out of Stock" :
                             currentStock <= item.MinStockLevel ? "Low Stock" : "In Stock"
            });
        }

        return stockDetails.OrderBy(s => s.ItemName).ToList();
    }
}

/// <summary>
/// Stock details for an inventory item
/// </summary>
public class InventoryStockDetails
{
    public int InventoryItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int TotalIn { get; set; }
    public int TotalOut { get; set; }
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsOutOfStock { get; set; }
    public string StockStatus { get; set; } = string.Empty;
}

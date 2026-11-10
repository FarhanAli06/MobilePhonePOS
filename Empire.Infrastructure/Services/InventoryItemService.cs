using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Inventory;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Services;

public class InventoryItemService : IInventoryItemService
{
    private readonly EmpireDbContext _context;

    public InventoryItemService(EmpireDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetInventoryItemsAsync(InventoryItemFilterRequest filter)
    {
        var query = _context.InventoryItems
            .AsNoTracking()
            .Where(i => !i.IsDeleted && i.ShopId == filter.ShopId);

        if (filter.BrandId.HasValue)
            query = query.Where(i => i.BrandId == filter.BrandId.Value);

        if (filter.DeviceCategoryId.HasValue)
            query = query.Where(i => i.DeviceCategoryId == filter.DeviceCategoryId.Value);

        if (filter.DeviceModelId.HasValue)
        {
            var compatibleModelToken = $"|{filter.DeviceModelId.Value}|";
            query = query.Where(i => i.DeviceModelId == filter.DeviceModelId.Value ||
                (i.Notes != null && i.Notes.Contains("COMPAT_MODELS:") && i.Notes.Contains(compatibleModelToken)));
        }

        if (filter.InventoryCategoryId.HasValue)
            query = query.Where(i => i.InventoryCategoryId == filter.InventoryCategoryId.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(i => i.IsActive == filter.IsActive.Value);
        else
            query = query.Where(i => i.IsActive);

        if (filter.LowStockOnly == true)
            query = query.Where(i => i.CurrentStock <= i.ReorderPoint);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(i =>
                i.Name.ToLower().Contains(term) ||
                i.SKU.ToLower().Contains(term) ||
                (i.Brand != null && i.Brand.Name.ToLower().Contains(term)) ||
                (i.DeviceModel != null && i.DeviceModel.Name.ToLower().Contains(term)) ||
                (i.InventoryCategory != null && i.InventoryCategory.Name.ToLower().Contains(term)));
        }

        // Project exactly the fields used by list screens. Loading every related
        // entity and every historical batch made large inventories progressively
        // slower as more shipments were received.
        var dtos = await query.OrderBy(i => i.Name).Select(item => new InventoryItemDto
        {
            Id = item.Id,
            ShopId = item.ShopId,
            BrandId = item.BrandId,
            BrandName = item.Brand != null ? item.Brand.Name : string.Empty,
            DeviceCategoryId = item.DeviceCategoryId,
            DeviceCategoryName = item.DeviceCategory != null ? item.DeviceCategory.Name : string.Empty,
            DeviceModelId = item.DeviceModelId,
            DeviceModelName = item.DeviceModel != null ? item.DeviceModel.Name : string.Empty,
            InventoryCategoryId = item.InventoryCategoryId,
            InventoryCategoryName = item.InventoryCategory != null ? item.InventoryCategory.Name : string.Empty,
            ItemId = item.ItemId,
            ItemName = item.Item != null ? item.Item.Name : null,
            CategoryId = item.CategoryId,
            CategoryName = item.Category != null ? item.Category.Name : null,
            Name = item.Name,
            Description = item.Description,
            SKU = item.SKU,
            Variant = item.Variant,
            Color = item.Color,
            CurrentStock = item.CurrentStock,
            ReorderPoint = item.ReorderPoint,
            MinStockLevel = item.MinStockLevel,
            ReorderQuantity = item.ReorderQuantity,
            CostPrice = item.CostPrice,
            RetailPrice = item.RetailPrice,
            WholesalePrice = item.WholesalePrice,
            EnableLowStockNotifications = item.EnableLowStockNotifications,
            IsActive = item.IsActive,
            Notes = item.Notes,
            CreatedDate = item.CreatedDate,
            ModifiedDate = item.ModifiedDate,
            LatestCost = item.InventoryBatches.Where(batch => !batch.IsDeleted)
                .OrderByDescending(batch => batch.ReceivedDate).ThenByDescending(batch => batch.Id)
                .Select(batch => (decimal?)batch.UnitCost).FirstOrDefault() ?? item.CostPrice,
            FifoCost = item.InventoryBatches.Where(batch => !batch.IsDeleted && batch.RemainingQuantity > 0)
                .OrderBy(batch => batch.ReceivedDate).ThenBy(batch => batch.Id)
                .Select(batch => (decimal?)batch.UnitCost).FirstOrDefault() ?? item.CostPrice,
            FifoBatchRemaining = item.InventoryBatches.Where(batch => !batch.IsDeleted && batch.RemainingQuantity > 0)
                .OrderBy(batch => batch.ReceivedDate).ThenBy(batch => batch.Id)
                .Select(batch => (int?)batch.RemainingQuantity).FirstOrDefault() ?? 0,
            FifoBatchReceivedDate = item.InventoryBatches.Where(batch => !batch.IsDeleted && batch.RemainingQuantity > 0)
                .OrderBy(batch => batch.ReceivedDate).ThenBy(batch => batch.Id)
                .Select(batch => (DateTime?)batch.ReceivedDate).FirstOrDefault(),
            AverageCost = item.InventoryBatches.Where(batch => !batch.IsDeleted && batch.RemainingQuantity > 0)
                .Sum(batch => (int?)batch.RemainingQuantity) > 0
                ? Math.Round(
                    item.InventoryBatches.Where(batch => !batch.IsDeleted && batch.RemainingQuantity > 0)
                        .Sum(batch => (decimal?)(batch.RemainingQuantity * batch.UnitCost))!.Value
                    / item.InventoryBatches.Where(batch => !batch.IsDeleted && batch.RemainingQuantity > 0)
                        .Sum(batch => (int?)batch.RemainingQuantity)!.Value, 2)
                : item.CostPrice
        }).ToListAsync();

        // ── Aggregated stock: group by Brand+Category+Model+InventoryCategory ──────
        // Build a lookup of (brandId, categoryId, modelId, invCatId) -> (totalStock, maxReorderPoint)
        // using ALL non-deleted items for this shop so the badge reflects the full group.
        var groupTotalsQuery = _context.InventoryItems.AsNoTracking()
            .Where(i => !i.IsDeleted && i.IsActive && i.ShopId == filter.ShopId)
            .AsQueryable();
        if (filter.BrandId.HasValue) groupTotalsQuery = groupTotalsQuery.Where(i => i.BrandId == filter.BrandId.Value);
        if (filter.DeviceCategoryId.HasValue) groupTotalsQuery = groupTotalsQuery.Where(i => i.DeviceCategoryId == filter.DeviceCategoryId.Value);
        if (filter.DeviceModelId.HasValue)
        {
            var compatibleModelToken = $"|{filter.DeviceModelId.Value}|";
            groupTotalsQuery = groupTotalsQuery.Where(i => i.DeviceModelId == filter.DeviceModelId.Value ||
                (i.Notes != null && i.Notes.Contains("COMPAT_MODELS:") && i.Notes.Contains(compatibleModelToken)));
        }
        if (filter.InventoryCategoryId.HasValue) groupTotalsQuery = groupTotalsQuery.Where(i => i.InventoryCategoryId == filter.InventoryCategoryId.Value);

        var allGroupTotals = await groupTotalsQuery
            .GroupBy(i => new { i.BrandId, i.DeviceCategoryId, i.DeviceModelId, i.InventoryCategoryId })
            .Select(g => new
            {
                g.Key.BrandId,
                g.Key.DeviceCategoryId,
                g.Key.DeviceModelId,
                g.Key.InventoryCategoryId,
                TotalStock    = g.Sum(i => i.CurrentStock),
                MaxReorder    = g.Max(i => i.ReorderPoint)
            })
            .ToListAsync();

        var groupLookup = allGroupTotals.ToDictionary(
            g => (g.BrandId, g.DeviceCategoryId, g.DeviceModelId, g.InventoryCategoryId));

        foreach (var dto in dtos)
        {
            var key = (dto.BrandId, dto.DeviceCategoryId, dto.DeviceModelId, dto.InventoryCategoryId);
            if (groupLookup.TryGetValue(key, out var grp))
            {
                dto.AggregatedStock      = grp.TotalStock;
                dto.IsLowStockAggregated = grp.TotalStock <= grp.MaxReorder;
            }
            else
            {
                dto.AggregatedStock      = dto.CurrentStock;
                dto.IsLowStockAggregated = dto.IsLowStock;
            }
        }

        // If LowStockOnly filter is active, re-apply it using the aggregated view
        if (filter.LowStockOnly == true)
            return dtos.Where(d => d.IsLowStockAggregated);

        return dtos;
    }

    public async Task<InventoryItemDto?> GetInventoryItemByIdAsync(int id, int shopId)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Brand)
            .Include(i => i.DeviceCategory)
            .Include(i => i.DeviceModel)
            .Include(i => i.InventoryCategory)
            .Include(i => i.Item)
            .Include(i => i.Category)
            .Include(i => i.InventoryBatches)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted && i.ShopId == shopId);

        return item == null ? null : MapToDto(item);
    }

    public async Task<InventoryItemDto> CreateInventoryItemAsync(CreateInventoryItemRequest request)
    {
        // Auto-generate SKU if not provided
        if (string.IsNullOrWhiteSpace(request.SKU))
            request.SKU = await GenerateSkuAsync(request.BrandId, request.DeviceCategoryId, request.DeviceModelId);

        var item = new InventoryItem
        {
            ShopId = request.ShopId,
            BrandId = request.BrandId,
            DeviceCategoryId = request.DeviceCategoryId,
            DeviceModelId = request.DeviceModelId,
            InventoryCategoryId = request.InventoryCategoryId,
            ItemId = request.ItemId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            SKU = request.SKU.Trim(),
            Variant = request.Variant?.Trim() ?? string.Empty,
            Color = request.Color?.Trim() ?? string.Empty,
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

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        if (item.CurrentStock > 0)
        {
            _context.InventoryBatches.Add(new InventoryBatch
            {
                InventoryItemId = item.Id,
                ReceivedQuantity = item.CurrentStock,
                RemainingQuantity = item.CurrentStock,
                UnitCost = item.CostPrice,
                Vendor = "Opening inventory",
                ReferenceNumber = "OPENING",
                ReceivedDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        // Reload with navigation properties
        await _context.Entry(item).Reference(i => i.Brand).LoadAsync();
        await _context.Entry(item).Reference(i => i.DeviceCategory).LoadAsync();
        await _context.Entry(item).Reference(i => i.DeviceModel).LoadAsync();
        await _context.Entry(item).Reference(i => i.InventoryCategory).LoadAsync();
        if (item.ItemId.HasValue)
            await _context.Entry(item).Reference(i => i.Item).LoadAsync();
        if (item.CategoryId.HasValue)
            await _context.Entry(item).Reference(i => i.Category).LoadAsync();

        return MapToDto(item);
    }

    public async Task<InventoryItemDto?> UpdateInventoryItemAsync(int id, UpdateInventoryItemRequest request, int shopId)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Brand)
            .Include(i => i.DeviceCategory)
            .Include(i => i.DeviceModel)
            .Include(i => i.InventoryCategory)
            .Include(i => i.Item)
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted && i.ShopId == shopId);
        if (item == null) return null;;

        if (request.BrandId.HasValue) item.BrandId = request.BrandId.Value;
        if (request.DeviceCategoryId.HasValue) item.DeviceCategoryId = request.DeviceCategoryId.Value;
        if (request.DeviceModelId.HasValue) item.DeviceModelId = request.DeviceModelId.Value;
        if (request.InventoryCategoryId.HasValue) item.InventoryCategoryId = request.InventoryCategoryId.Value;
        if (request.ItemId.HasValue) item.ItemId = request.ItemId;
        if (request.CategoryId.HasValue) item.CategoryId = request.CategoryId;
        if (request.Name != null) item.Name = request.Name.Trim();
        if (request.Description != null) item.Description = request.Description.Trim();
        if (request.SKU != null) item.SKU = request.SKU.Trim();
        if (request.Variant != null) item.Variant = request.Variant.Trim();
        if (request.Color != null) item.Color = request.Color.Trim();
        if (request.ReorderPoint.HasValue) item.ReorderPoint = request.ReorderPoint.Value;
        if (request.CostPrice.HasValue) item.CostPrice = request.CostPrice.Value;
        if (request.RetailPrice.HasValue) item.RetailPrice = request.RetailPrice.Value;
        if (request.WholesalePrice.HasValue) item.WholesalePrice = request.WholesalePrice.Value;
        if (request.EnableLowStockNotifications.HasValue) item.EnableLowStockNotifications = request.EnableLowStockNotifications.Value;
        if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;
        if (request.Notes != null) item.Notes = request.Notes.Trim();

        item.UpdatedDate = DateTime.UtcNow;
        item.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload navigation properties after update
        await _context.Entry(item).Reference(i => i.Brand).LoadAsync();
        await _context.Entry(item).Reference(i => i.DeviceCategory).LoadAsync();
        await _context.Entry(item).Reference(i => i.DeviceModel).LoadAsync();
        await _context.Entry(item).Reference(i => i.InventoryCategory).LoadAsync();

        return MapToDto(item);
    }

    public async Task<bool> DeleteInventoryItemAsync(int id, int shopId)
    {
        var item = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted && i.ShopId == shopId);

        if (item == null) return false;

        item.IsDeleted = true;
        item.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> GenerateSkuAsync(int brandId, int deviceCategoryId, int deviceModelId)
    {
        var brand = await _context.Brands.FindAsync(brandId);
        var category = await _context.DeviceCategories.FindAsync(deviceCategoryId);
        var model = await _context.DeviceModels.FindAsync(deviceModelId);

        var brandCode = brand?.Name?.Length >= 3 ? brand.Name.Substring(0, 3).ToUpper() : "UNK";
        var catCode = category?.Name?.Length >= 3 ? category.Name.Substring(0, 3).ToUpper() : "UNK";
        var modelCode = model?.Name?.Length >= 3 ? model.Name.Substring(0, 3).ToUpper() : "UNK";

        // Find the next sequence number for this combination
        var prefix = $"{brandCode}-{catCode}-{modelCode}-";
        var existingCount = await _context.InventoryItems
            .Where(i => i.SKU.StartsWith(prefix) && !i.IsDeleted)
            .CountAsync();

        return $"{prefix}{(existingCount + 1):D4}";
    }

    public async Task<bool> SkuExistsAsync(string sku, int shopId, int? excludeItemId = null)
    {
        var query = _context.InventoryItems
            .Where(i => i.SKU == sku && !i.IsDeleted && i.ShopId == shopId);

        if (excludeItemId.HasValue)
            query = query.Where(i => i.Id != excludeItemId.Value);

        return await query.AnyAsync();
    }

    public async Task<InventoryBatchDto> ReceiveBatchAsync(int id, ReceiveInventoryBatchRequest request, int shopId)
    {
        if (request.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        if (request.UnitCost < 0) throw new ArgumentException("Unit cost cannot be negative.");

        var item = await _context.InventoryItems
            .Include(i => i.InventoryBatches)
            .FirstOrDefaultAsync(i => i.Id == id && i.ShopId == shopId && !i.IsDeleted)
            ?? throw new InvalidOperationException("Inventory item not found.");

        var batch = new InventoryBatch
        {
            InventoryItemId = id,
            ReceivedQuantity = request.Quantity,
            RemainingQuantity = request.Quantity,
            UnitCost = request.UnitCost,
            Vendor = request.Vendor?.Trim() ?? string.Empty,
            ReferenceNumber = request.ReferenceNumber?.Trim() ?? string.Empty,
            ReceivedDate = request.ReceivedDate ?? DateTime.UtcNow,
            Notes = request.Notes?.Trim() ?? string.Empty,
            CreatedDate = DateTime.UtcNow
        };

        _context.InventoryBatches.Add(batch);
        item.CurrentStock += request.Quantity;
        item.CostPrice = request.UnitCost; // legacy field now means latest received cost
        item.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapBatch(batch);
    }

    public async Task<IReadOnlyList<InventoryBatchDto>> GetBatchesAsync(int id, int shopId)
    {
        var ownsItem = await _context.InventoryItems.AnyAsync(i => i.Id == id && i.ShopId == shopId && !i.IsDeleted);
        if (!ownsItem) return Array.Empty<InventoryBatchDto>();
        return await _context.InventoryBatches
            .Where(b => b.InventoryItemId == id && !b.IsDeleted)
            .OrderBy(b => b.ReceivedDate).ThenBy(b => b.Id)
            .Select(b => new InventoryBatchDto
            {
                Id = b.Id, InventoryItemId = b.InventoryItemId, ReceivedQuantity = b.ReceivedQuantity,
                RemainingQuantity = b.RemainingQuantity, UnitCost = b.UnitCost, Vendor = b.Vendor,
                ReferenceNumber = b.ReferenceNumber, ReceivedDate = b.ReceivedDate, Notes = b.Notes
            }).ToListAsync();
    }

    public async Task<int> ResetStockAsync(int shopId)
    {
        var items = await _context.InventoryItems
            .Include(item => item.InventoryBatches)
            .Where(item => item.ShopId == shopId && !item.IsDeleted)
            .ToListAsync();

        var itemIds = items.Select(item => item.Id).ToList();
        if (itemIds.Count == 0) return 0;

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.CurrentStock = 0;
            item.UpdatedDate = now;
            item.ModifiedDate = now;
            foreach (var batch in item.InventoryBatches.Where(batch => !batch.IsDeleted))
            {
                batch.RemainingQuantity = 0;
                batch.IsDeleted = true;
                batch.ModifiedDate = now;
            }
        }

        var transactions = await _context.InventoryTransactions
            .Where(transaction => itemIds.Contains(transaction.InventoryItemId))
            .ToListAsync();
        var movements = await _context.StockMovements
            .Where(movement => itemIds.Contains(movement.InventoryItemId))
            .ToListAsync();
        _context.InventoryTransactions.RemoveRange(transactions);
        _context.StockMovements.RemoveRange(movements);
        await _context.SaveChangesAsync();
        return items.Count;
    }

    private static InventoryBatchDto MapBatch(InventoryBatch b) => new()
    {
        Id = b.Id, InventoryItemId = b.InventoryItemId, ReceivedQuantity = b.ReceivedQuantity,
        RemainingQuantity = b.RemainingQuantity, UnitCost = b.UnitCost, Vendor = b.Vendor,
        ReferenceNumber = b.ReferenceNumber, ReceivedDate = b.ReceivedDate, Notes = b.Notes
    };

    private static InventoryItemDto MapToDto(InventoryItem item)
    {
        var batches = item.InventoryBatches?.Where(b => !b.IsDeleted).OrderBy(b => b.ReceivedDate).ThenBy(b => b.Id).ToList()
                      ?? new List<InventoryBatch>();
        var openBatches = batches.Where(b => b.RemainingQuantity > 0).ToList();
        var fifo = openBatches.FirstOrDefault();
        var latest = batches.OrderByDescending(b => b.ReceivedDate).ThenByDescending(b => b.Id).FirstOrDefault();
        var remainingTotal = openBatches.Sum(b => b.RemainingQuantity);
        var remainingValue = openBatches.Sum(b => b.RemainingQuantity * b.UnitCost);
        return new InventoryItemDto
        {
        Id = item.Id,
        ShopId = item.ShopId,
        ShopName = item.Shop?.Name ?? string.Empty,
        BrandId = item.BrandId,
        BrandName = item.Brand?.Name ?? string.Empty,
        DeviceCategoryId = item.DeviceCategoryId,
        DeviceCategoryName = item.DeviceCategory?.Name ?? string.Empty,
        DeviceModelId = item.DeviceModelId,
        DeviceModelName = item.DeviceModel?.Name ?? string.Empty,
        InventoryCategoryId = item.InventoryCategoryId,
        InventoryCategoryName = item.InventoryCategory?.Name ?? string.Empty,
        ItemId = item.ItemId,
        ItemName = item.Item?.Name,
        CategoryId = item.CategoryId,
        CategoryName = item.Category?.Name,
        Name = item.Name,
        Description = item.Description,
        SKU = item.SKU,
        Variant = item.Variant,
        Color = item.Color,
        CurrentStock = item.CurrentStock,
        ReorderPoint = item.ReorderPoint,
        MinStockLevel = item.MinStockLevel,
        ReorderQuantity = item.ReorderQuantity,
        CostPrice = item.CostPrice,
        RetailPrice = item.RetailPrice,
        WholesalePrice = item.WholesalePrice,
        EnableLowStockNotifications = item.EnableLowStockNotifications,
        IsActive = item.IsActive,
        Notes = item.Notes,
        CreatedDate = item.CreatedDate,
        ModifiedDate = item.ModifiedDate,
        LatestCost = latest?.UnitCost ?? item.CostPrice,
        AverageCost = remainingTotal > 0 ? Math.Round(remainingValue / remainingTotal, 2) : item.CostPrice,
        FifoCost = fifo?.UnitCost ?? item.CostPrice,
        FifoBatchRemaining = fifo?.RemainingQuantity ?? 0,
        FifoBatchReceivedDate = fifo?.ReceivedDate,
        Batches = batches.Select(MapBatch).ToList()
        };
    }
}

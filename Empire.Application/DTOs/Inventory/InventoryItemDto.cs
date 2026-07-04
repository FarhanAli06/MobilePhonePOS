namespace Empire.Application.DTOs.Inventory;

public class InventoryItemDto
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;

    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;

    public int DeviceCategoryId { get; set; }
    public string DeviceCategoryName { get; set; } = string.Empty;

    public int DeviceModelId { get; set; }
    public string DeviceModelName { get; set; } = string.Empty;

    public int InventoryCategoryId { get; set; }
    public string InventoryCategoryName { get; set; } = string.Empty;

    public int? ItemId { get; set; }
    public string? ItemName { get; set; }

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;

    public int CurrentStock { get; set; }
    public int ReorderPoint { get; set; }
    public int MinStockLevel { get; set; }
    public int ReorderQuantity { get; set; }

    public decimal CostPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }

    public bool EnableLowStockNotifications { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Per-item low stock flag (CurrentStock &lt;= ReorderPoint).</summary>
    public bool IsLowStock => CurrentStock <= ReorderPoint;

    /// <summary>
    /// Total stock across all InventoryItems that share the same
    /// BrandId + DeviceCategoryId + DeviceModelId + InventoryCategoryId group.
    /// Populated by the service layer after querying.
    /// </summary>
    public int AggregatedStock { get; set; }

    /// <summary>
    /// True when the group's aggregated stock is at or below the highest
    /// ReorderPoint in the group. Use this for the Low-Stock badge.
    /// </summary>
    public bool IsLowStockAggregated { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

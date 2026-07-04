namespace Empire.Web.DTOs.Inventory;

/// <summary>
/// Web-layer DTO that maps 1:1 to the API's Application.DTOs.Inventory.InventoryItemDto response.
/// Field names use camelCase JSON deserialization (ASP.NET Core default).
/// </summary>
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
    public bool IsActive { get; set; } = true;
    public bool IsLowStock { get; set; }
    public int AggregatedStock { get; set; }
    public bool IsLowStockAggregated { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // ── Backward-compat aliases used by MappingHelper and other Web code ──
    public string? Brand => BrandName;
    public string? Category => DeviceCategoryName;
    public string? DeviceCategory => DeviceCategoryName;
    public string? Model => DeviceModelName;
    public string? DeviceModel => DeviceModelName;
    public string? InventoryCategory => InventoryCategoryName;
    public int? Stock => CurrentStock;
}

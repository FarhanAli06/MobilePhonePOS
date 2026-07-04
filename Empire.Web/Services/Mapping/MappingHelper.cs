using Empire.Web.DTOs.Inventory;

namespace Empire.Web.Services.Mapping;

/// <summary>
/// Implementation of mapping helper service
/// </summary>
public class MappingHelper : IMappingHelper
{
    /// <summary>
    /// Maps inventory item DTO to anonymous object for JSON response
    /// </summary>
    public object MapInventoryItemToAnonymous(InventoryItemDto item)
    {
        return new
        {
            id = item.Id,
            name = item.Name,
            sku = item.SKU,
            description = item.Description,
            brandId = item.BrandId,
            brandName = item.Brand ?? "Unknown",
            deviceCategoryId = item.DeviceCategoryId,
            deviceCategoryName = item.DeviceCategory ?? "Unknown",
            deviceModelId = item.DeviceModelId,
            deviceModelName = item.DeviceModel ?? "Unknown",
            inventoryCategoryId = item.InventoryCategoryId,
            inventoryCategoryName = item.InventoryCategory ?? "Unknown",
            currentStock = item.CurrentStock,
            reorderPoint = item.ReorderPoint,
            costPrice = item.CostPrice,
            retailPrice = item.RetailPrice,
            wholesalePrice = item.WholesalePrice,
            enableLowStockNotifications = item.EnableLowStockNotifications,
            isActive = item.IsActive,
            notes = item.Notes,
            createdDate = item.CreatedDate,
            aggregatedStock = item.AggregatedStock,
            isLowStockAggregated = item.IsLowStockAggregated
        };
    }
    
    /// <summary>
    /// Maps a collection of inventory items to anonymous objects
    /// </summary>
    public IEnumerable<object> MapInventoryItemsToAnonymous(IEnumerable<InventoryItemDto> items)
    {
        return items.Select(MapInventoryItemToAnonymous);
    }
    
    /// <summary>
    /// Maps simple ID/Name pairs to anonymous objects
    /// </summary>
    public object MapToIdName(int id, string name)
    {
        return new { Id = id, Name = name };
    }
    
    /// <summary>
    /// Maps POS product search results to anonymous objects
    /// </summary>
    public object MapPOSProductToAnonymous(dynamic product)
    {
        return new
        {
            id = product.Id,
            name = product.Name,
            brand = product.Brand,
            category = product.Category,
            sku = product.Sku,
            price = product.Price,
            wholesalePrice = product.WholesalePrice,
            stock = product.Stock,
            type = product.Type,
            description = product.Description
        };
    }
    
    /// <summary>
    /// Maps customer search results to anonymous objects
    /// </summary>
    public object MapCustomerToAnonymous(dynamic customer)
    {
        return new
        {
            id = customer.Id,
            name = $"{customer.FirstName} {customer.LastName}",
            phone = customer.Phone,
            email = customer.Email,
            type = "customer"
        };
    }
    
    /// <summary>
    /// Maps repair search results to anonymous objects
    /// </summary>
    public object MapRepairToAnonymous(dynamic repair)
    {
        return new
        {
            id = repair.Id,
            customerName = repair.CustomerName,
            deviceBrand = repair.DeviceBrand,
            deviceModel = repair.DeviceModel,
            issue = repair.Issue,
            status = repair.Status,
            estimatedCost = repair.EstimatedCost,
            type = "repair"
        };
    }
}

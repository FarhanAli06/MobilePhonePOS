using Empire.Web.DTOs.Inventory;

namespace Empire.Web.Services.InventoryManagement;

public interface IInventoryManagementApiService
{
    Task<List<InventoryItemDto>> GetInventoryItemsAsync(
        int shopId,
        int? brandId = null,
        int? deviceCategoryId = null,
        int? deviceModelId = null,
        int? inventoryCategoryId = null,
        string? stockStatus = null);

    Task<InventoryItemDto?> GetInventoryItemByIdAsync(int id, int shopId);
    
    Task<InventoryItemDto> CreateInventoryItemAsync(InventoryItemDto item, int userId);
    
    Task<InventoryItemDto> UpdateInventoryItemAsync(InventoryItemDto item);
    
    Task<bool> DeleteInventoryItemAsync(int id, int shopId);
    
    Task<string> GenerateSkuAsync(int brandId, int deviceCategoryId, int deviceModelId);
    
    Task<bool> SkuExistsAsync(string sku, int shopId, int? excludeItemId = null);
    
    /// <summary>
    /// Maps inventory items to anonymous objects for JSON response
    /// </summary>
    IEnumerable<object> MapInventoryItemsForResponse(IEnumerable<InventoryItemDto> items);
}

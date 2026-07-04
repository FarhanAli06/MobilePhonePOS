using System;
using Empire.Web.Services.Http;
using Empire.Web.Services.Mapping;
using Empire.Web.Services.Inventory;
using Empire.Web.DTOs.Inventory;

namespace Empire.Web.Services.InventoryManagement;

public class InventoryManagementApiService : IInventoryManagementApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly IInventoryApiService _inventoryApi;
    private readonly IMappingHelper _mappingHelper;
    private readonly ILogger<InventoryManagementApiService> _logger;
    private const string BaseEndpoint = "/api/inventory-items";

    public InventoryManagementApiService(
        IHttpClientService httpClient,
        IInventoryApiService inventoryApi,
        IMappingHelper mappingHelper,
        ILogger<InventoryManagementApiService> logger)
    {
        _httpClient = httpClient;
        _inventoryApi = inventoryApi;
        _mappingHelper = mappingHelper;
        _logger = logger;
    }

    public async Task<List<InventoryItemDto>> GetInventoryItemsAsync(
        int shopId,
        int? brandId = null,
        int? deviceCategoryId = null,
        int? deviceModelId = null,
        int? inventoryCategoryId = null,
        string? stockStatus = null)
    {
        try
        {
            var queryParams = new List<string> { $"shopId={shopId}" };
            
            if (brandId.HasValue)
                queryParams.Add($"brandId={brandId.Value}");
            if (deviceCategoryId.HasValue)
                queryParams.Add($"deviceCategoryId={deviceCategoryId.Value}");
            if (deviceModelId.HasValue)
                queryParams.Add($"deviceModelId={deviceModelId.Value}");
            if (inventoryCategoryId.HasValue)
                queryParams.Add($"inventoryCategoryId={inventoryCategoryId.Value}");
            if (!string.IsNullOrEmpty(stockStatus))
            {
                // Translate UI stockStatus value to the API's lowStockOnly parameter
                if (stockStatus.Equals("low_stock", StringComparison.OrdinalIgnoreCase))
                    queryParams.Add("lowStockOnly=true");
                else
                    queryParams.Add($"stockStatus={stockStatus}");
            }

            var endpoint = $"{BaseEndpoint}?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync<List<InventoryItemDto>>(endpoint);
            
            return response ?? new List<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory items for shop {ShopId}", shopId);
            return new List<InventoryItemDto>();
        }
    }

    public async Task<InventoryItemDto?> GetInventoryItemByIdAsync(int id, int shopId)
    {
        try
        {
            var response = await _httpClient.GetAsync<InventoryItemDto>($"{BaseEndpoint}/{id}?shopId={shopId}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory item {Id}", id);
            return null;
        }
    }

    public async Task<InventoryItemDto> CreateInventoryItemAsync(InventoryItemDto item, int userId)
    {
        try
        {
            var request = new CreateInventoryItemRequestDto
            {
                ShopId = item.ShopId,
                ItemId = item.ItemId,
                CategoryId = item.CategoryId,
                Name = item.Name,
                SKU = item.SKU,
                Description = item.Description,
                BrandId = item.BrandId,
                DeviceCategoryId = item.DeviceCategoryId,
                DeviceModelId = item.DeviceModelId,
                InventoryCategoryId = item.InventoryCategoryId,
                CurrentStock = item.CurrentStock,
                ReorderPoint = item.ReorderPoint,
                CostPrice = item.CostPrice,
                RetailPrice = item.RetailPrice,
                WholesalePrice = item.WholesalePrice,
                EnableLowStockNotifications = item.EnableLowStockNotifications,
                IsActive = item.IsActive,
                Notes = item.Notes
            };

            var response = await _httpClient.PostAsync<CreateInventoryItemRequestDto, InventoryItemDto>(
                $"{BaseEndpoint}?userId={userId}", request);
            
            _logger.LogInformation("Created inventory item {ItemId} with initial stock {Stock}", 
                response?.Id, item.CurrentStock);
            
            return response ?? item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory item");
            throw;
        }
    }

    public async Task<InventoryItemDto> UpdateInventoryItemAsync(InventoryItemDto item)
    {
        try
        {
            var request = new UpdateInventoryItemRequestDto
            {
                ItemId = item.ItemId,
                CategoryId = item.CategoryId,
                Name = item.Name,
                SKU = item.SKU,
                Description = item.Description,
                BrandId = item.BrandId,
                DeviceCategoryId = item.DeviceCategoryId,
                DeviceModelId = item.DeviceModelId,
                InventoryCategoryId = item.InventoryCategoryId,
                ReorderPoint = item.ReorderPoint,
                CostPrice = item.CostPrice,
                RetailPrice = item.RetailPrice,
                WholesalePrice = item.WholesalePrice,
                EnableLowStockNotifications = item.EnableLowStockNotifications,
                IsActive = item.IsActive,
                Notes = item.Notes
            };

            var response = await _httpClient.PutAsync<UpdateInventoryItemRequestDto, InventoryItemDto>(
                $"{BaseEndpoint}/{item.Id}", request);
            
            _logger.LogInformation("Updated inventory item {ItemId}", item.Id);
            
            return response ?? item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item {ItemId}", item.Id);
            throw;
        }
    }

    public async Task<bool> DeleteInventoryItemAsync(int id, int shopId)
    {
        try
        {
            var result = await _httpClient.DeleteAsync($"{BaseEndpoint}/{id}?shopId={shopId}");
            
            _logger.LogInformation("Deleted (soft) inventory item {ItemId}", id);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item {ItemId}", id);
            return false;
        }
    }

    public async Task<string> GenerateSkuAsync(int brandId, int deviceCategoryId, int deviceModelId)
    {
        try
        {
            var url = $"{BaseEndpoint}/generate-sku?brandId={brandId}&categoryId={deviceCategoryId}&modelId={deviceModelId}";
            var response = await _httpClient.GetAsync<string>(url);

            if (response == null)
            {
                // API call failed (non-2xx or deserialization error).
                // Check the [WEB→API] log lines above for the exact HTTP status code.
                // Use a timestamp-based fallback so the item can still be saved;
                // the user can correct the SKU manually afterwards.
                _logger.LogWarning(
                    "[GenerateSku] API returned null for brandId={BrandId} categoryId={CategoryId} modelId={ModelId}. "
                    + "Using timestamp fallback. Check [WEB\u2192API] log lines for the HTTP error.",
                    brandId, deviceCategoryId, deviceModelId);
                return $"INV-{brandId}-{deviceCategoryId}-{deviceModelId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[GenerateSku] Exception for brandId={BrandId} categoryId={CategoryId} modelId={ModelId}",
                brandId, deviceCategoryId, deviceModelId);
            return $"INV-{brandId}-{deviceCategoryId}-{deviceModelId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }

    public async Task<bool> SkuExistsAsync(string sku, int shopId, int? excludeItemId = null)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/sku-exists?sku={Uri.EscapeDataString(sku)}&shopId={shopId}";
            if (excludeItemId.HasValue)
                endpoint += $"&excludeItemId={excludeItemId.Value}";

            var response = await _httpClient.GetAsync<bool>(endpoint);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SKU existence");
            return false;
        }
    }

    /// <summary>
    /// Maps inventory items to anonymous objects for JSON response
    /// </summary>
    public IEnumerable<object> MapInventoryItemsForResponse(IEnumerable<InventoryItemDto> items)
    {
        return _mappingHelper.MapInventoryItemsToAnonymous(items);
    }
}

using Empire.Web.DTOs.Brand;
using Empire.Web.DTOs.Inventory;
using Empire.Web.DTOs.Category;
using Empire.Web.Services.Http;
using Empire.Web.Services.Inventory;

namespace Empire.Web.Services.API;

/// <summary>
/// API service for Inventory operations
/// </summary>
public class InventoryApiService : IInventoryApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<InventoryApiService> _logger;
    private const string BaseEndpoint = "/api/inventory-items";

    public InventoryApiService(
        IHttpClientService httpClient,
        ILogger<InventoryApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<InventoryItemDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<InventoryItemDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory item {Id}", id);
            return null;
        }
    }

    public async Task<InventoryItemDto?> CreateAsync(CreateInventoryItemRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<CreateInventoryItemRequestDto, InventoryItemDto>(BaseEndpoint, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory item");
            return null;
        }
    }

    public async Task<InventoryItemDto?> UpdateAsync(int id, UpdateInventoryItemRequestDto request)
    {
        try
        {
            return await _httpClient.PutAsync<UpdateInventoryItemRequestDto, InventoryItemDto>($"{BaseEndpoint}/{id}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory item {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            return await _httpClient.DeleteAsync($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory item {Id}", id);
            return false;
        }
    }

    // Brand endpoints
    public async Task<List<BrandDto>?> GetBrandsAsync()
    {
        try
        {
            return await _httpClient.GetAsync<List<BrandDto>>($"{BaseEndpoint}/brands");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands");
            return null;
        }
    }

    // Category endpoints
    public async Task<List<CategoryDto>?> GetCategoriesAsync(int? brandId = null)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/categories";
            if (brandId.HasValue)
                endpoint += $"?brandId={brandId.Value}";

            return await _httpClient.GetAsync<List<CategoryDto>>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return null;
        }
    }

    // Model endpoints
    public async Task<List<ModelDto>?> GetModelsAsync(int? brandId = null, int? categoryId = null)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/models?";
            var queryParams = new List<string>();

            if (brandId.HasValue)
                queryParams.Add($"brandId={brandId.Value}");
            if (categoryId.HasValue)
                queryParams.Add($"categoryId={categoryId.Value}");

            endpoint += string.Join("&", queryParams);

            return await _httpClient.GetAsync<List<ModelDto>>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting models");
            return null;
        }
    }

    // Parts endpoints
    public async Task<List<PartDto>?> GetPartsAsync(int? brandId = null, int? categoryId = null, int? modelId = null)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/parts?";
            var queryParams = new List<string>();

            if (brandId.HasValue)
                queryParams.Add($"brandId={brandId.Value}");
            if (categoryId.HasValue)
                queryParams.Add($"categoryId={categoryId.Value}");
            if (modelId.HasValue)
                queryParams.Add($"modelId={modelId.Value}");

            endpoint += string.Join("&", queryParams);

            return await _httpClient.GetAsync<List<PartDto>>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parts");
            return null;
        }
    }

    public async Task<List<InventoryItemDto>?> SearchAsync(string searchTerm)
    {
        try
        {
            return await _httpClient.GetAsync<List<InventoryItemDto>>($"{BaseEndpoint}?search={Uri.EscapeDataString(searchTerm)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching inventory with term: {SearchTerm}", searchTerm);
            return null;
        }
    }

    public async Task<List<InventoryItemDto>?> GetLowStockAsync(int shopId = 0, int threshold = 10)
    {
        try
        {
            return await _httpClient.GetAsync<List<InventoryItemDto>>($"{BaseEndpoint}?lowStockOnly=true&shopId={shopId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock items");
            return null;
        }
    }

    public async Task<IEnumerable<InventoryItemDto>> GetLowStockItemsAsync(int shopId = 0, int threshold = 10)
    {
        try
        {
            var result = await GetLowStockAsync(shopId, threshold);
            return result ?? Enumerable.Empty<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock items");
            return Enumerable.Empty<InventoryItemDto>();
        }
    }

    public async Task<IEnumerable<InventoryItemDto>> GetInventoryAsync(int shopId)
    {
        try
        {
            var result = await _httpClient.GetAsync<List<InventoryItemDto>>($"{BaseEndpoint}?shopId={shopId}");
            return result ?? Enumerable.Empty<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory for shop {ShopId}", shopId);
            return Enumerable.Empty<InventoryItemDto>();
        }
    }
}




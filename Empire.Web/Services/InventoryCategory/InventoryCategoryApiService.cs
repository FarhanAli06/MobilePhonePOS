using Empire.Web.DTOs.InventoryCategory;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.API;

/// <summary>
/// API service for Inventory Category operations
/// </summary>
public class InventoryCategoryApiService : IInventoryCategoryApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<InventoryCategoryApiService> _logger;
    private const string BaseEndpoint = "/api/inventorycategories";

    public InventoryCategoryApiService(
        IHttpClientService httpClient,
        ILogger<InventoryCategoryApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<InventoryCategoryDto>?> GetAllAsync()
    {
        try
        {
            return await _httpClient.GetAsync<List<InventoryCategoryDto>>(BaseEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all inventory categories");
            return null;
        }
    }

    public async Task<InventoryCategoryDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<InventoryCategoryDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory category {Id}", id);
            return null;
        }
    }

    public async Task<InventoryCategoryDto?> CreateAsync(CreateInventoryCategoryRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<CreateInventoryCategoryRequestDto, InventoryCategoryDto>(BaseEndpoint, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory category");
            return null;
        }
    }

    public async Task<InventoryCategoryDto?> UpdateAsync(int id, UpdateInventoryCategoryRequestDto request)
    {
        try
        {
            return await _httpClient.PutAsync<UpdateInventoryCategoryRequestDto, InventoryCategoryDto>($"{BaseEndpoint}/{id}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory category {Id}", id);
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
            _logger.LogError(ex, "Error deleting inventory category {Id}", id);
            return false;
        }
    }
}

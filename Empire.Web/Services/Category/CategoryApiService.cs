using Empire.Web.DTOs.Category;
using Empire.Web.Services.Http;
using Empire.Web.Services.Category;

namespace Empire.Web.Services.API;

/// <summary>
/// API service for Category operations (for lookup categories)
/// </summary>
public class CategoryApiService : ICategoryApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<CategoryApiService> _logger;
    private const string BaseEndpoint = "/api/categories";

    public CategoryApiService(
        IHttpClientService httpClient,
        ILogger<CategoryApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<CategoryDto>?> GetAllAsync(string? type = null)
    {
        try
        {
            var endpoint = BaseEndpoint;
            if (!string.IsNullOrEmpty(type))
                endpoint += $"?categoryType={Uri.EscapeDataString(type)}";
                
            return await _httpClient.GetAsync<List<CategoryDto>>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return null;
        }
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<CategoryDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {Id}", id);
            return null;
        }
    }

    public async Task<CategoryDto?> CreateAsync(CreateCategoryRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<CreateCategoryRequestDto, CategoryDto>(BaseEndpoint, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return null;
        }
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequestDto request)
    {
        try
        {
            return await _httpClient.PutAsync<UpdateCategoryRequestDto, CategoryDto>($"{BaseEndpoint}/{id}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
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
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return false;
        }
    }
}

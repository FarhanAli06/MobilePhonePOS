using Empire.Web.DTOs.Brand;
using Empire.Web.Services.Brand;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.API;

/// <summary>
/// API service for Brand operations
/// </summary>
public class BrandApiService : IBrandApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<BrandApiService> _logger;
    private const string BaseEndpoint = "/api/brands";

    public BrandApiService(
        IHttpClientService httpClient,
        ILogger<BrandApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<BrandDto>?> GetAllAsync()
    {
        try
        {
            return await _httpClient.GetAsync<List<BrandDto>>(BaseEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all brands");
            return null;
        }
    }

    public async Task<object?> GetAllSortedAsync()
    {
        try
        {
            var brands = await GetAllAsync();
            if (brands == null)
            {
                return null;
            }

            var data = brands
                .OrderBy(b => b.DisplayOrder)
                .ThenBy(b => b.Name)
                .Select(b => new
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    DisplayOrder = b.DisplayOrder,
                    IsActive = b.IsActive,
                    CreatedDate = b.CreatedDate
                });

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sorted brands");
            return null;
        }
    }

    public async Task<BrandDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<BrandDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brand {Id}", id);
            return null;
        }
    }

    public async Task<BrandDto?> CreateAsync(CreateBrandRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<CreateBrandRequestDto, BrandDto>(BaseEndpoint, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating brand");
            return null;
        }
    }

    public async Task<BrandDto?> UpdateAsync(int id, UpdateBrandRequestDto request)
    {
        try
        {
            return await _httpClient.PutAsync<UpdateBrandRequestDto, BrandDto>($"{BaseEndpoint}/{id}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating brand {Id}", id);
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
            _logger.LogError(ex, "Error deleting brand {Id}", id);
            return false;
        }
    }

    public async Task<bool> CheckDuplicateNameAsync(string name, int? excludeId = null)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/check-duplicate?name={Uri.EscapeDataString(name)}";
            if (excludeId.HasValue)
                endpoint += $"&excludeId={excludeId.Value}";
            return await _httpClient.GetAsync<bool>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate brand name");
            return false;
        }
    }

    public async Task<List<BrandDto>> GetActiveBrandsAsync()
    {
        try
        {
            var result = await _httpClient.GetAsync<List<BrandDto>>($"{BaseEndpoint}/active");
            return result ?? new List<BrandDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active brands");
            return new List<BrandDto>();
        }
    }
}

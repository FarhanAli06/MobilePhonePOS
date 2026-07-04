using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.DeviceCategory;
using Empire.Web.Services.DeviceCategory;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.API;

/// <summary>
/// API service for Device Category operations
/// </summary>
public class DeviceCategoryApiService : IDeviceCategoryApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<DeviceCategoryApiService> _logger;
    private const string BaseEndpoint = "/api/devicecategories";

    public DeviceCategoryApiService(
        IHttpClientService httpClient,
        ILogger<DeviceCategoryApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<DeviceCategoryDto>?> GetAllAsync()
    {
        try
        {
            return await _httpClient.GetAsync<List<DeviceCategoryDto>>(BaseEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all device categories");
            return null;
        }
    }

    public async Task<DeviceCategoryDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<DeviceCategoryDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device category {Id}", id);
            return null;
        }
    }

    public async Task<List<DeviceCategoryDto>?> GetByBrandAsync(int brandId)
    {
        try
        {
            return await _httpClient.GetAsync<List<DeviceCategoryDto>>($"{BaseEndpoint}/by-brand/{brandId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device categories for brand {BrandId}", brandId);
            return null;
        }
    }

    public async Task<DeviceCategoryDto?> CreateAsync(CreateDeviceCategoryRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<CreateDeviceCategoryRequestDto, DeviceCategoryDto>(BaseEndpoint, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device category");
            return null;
        }
    }

    public async Task<DeviceCategoryDto?> UpdateAsync(int id, UpdateDeviceCategoryRequestDto request)
    {
        try
        {
            return await _httpClient.PutAsync<UpdateDeviceCategoryRequestDto, DeviceCategoryDto>($"{BaseEndpoint}/{id}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device category {Id}", id);
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
            _logger.LogError(ex, "Error deleting device category {Id}", id);
            return false;
        }
    }

    public async Task<List<DeviceCategoryDto>> GetByBrandIdAsync(int brandId)
    {
        try
        {
            var result = await GetByBrandAsync(brandId);
            return result ?? new List<DeviceCategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device categories for brand {BrandId}", brandId);
            return new List<DeviceCategoryDto>();
        }
    }

    public async Task<bool> CheckDuplicateNameAsync(string name, int brandId, int? excludeId = null)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/check-duplicate?name={Uri.EscapeDataString(name)}&brandId={brandId}";
            if (excludeId.HasValue)
                endpoint += $"&excludeId={excludeId.Value}";
            return await _httpClient.GetAsync<bool>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate device category name");
            return false;
        }
    }
}

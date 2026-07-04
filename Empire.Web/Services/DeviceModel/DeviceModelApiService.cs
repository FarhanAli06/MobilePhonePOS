using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.DeviceModel;
using Empire.Web.Services.DeviceModel;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.API;

/// <summary>
/// API service for Device Model operations
/// </summary>
public class DeviceModelApiService : IDeviceModelApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<DeviceModelApiService> _logger;
    private const string BaseEndpoint = "/api/devicemodels";

    public DeviceModelApiService(
        IHttpClientService httpClient,
        ILogger<DeviceModelApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<DeviceModelDto>?> GetAllAsync()
    {
        try
        {
            return await _httpClient.GetAsync<List<DeviceModelDto>>(BaseEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all device models");
            return null;
        }
    }

    public async Task<object?> GetAllSortedAsync()
    {
        try
        {
            var models = await GetAllAsync();
            if (models == null)
            {
                return null;
            }

            var data = models
                .OrderBy(dm => dm.BrandName)
                .ThenBy(dm => dm.CategoryName)
                .ThenBy(dm => dm.DisplayOrder)
                .ThenBy(dm => dm.Name)
                .Select(dm => new
                {
                    Id = dm.Id,
                    Name = dm.Name,
                    BrandId = dm.BrandId,
                    BrandName = dm.BrandName,
                    DeviceCategoryId = dm.DeviceCategoryId,
                    CategoryName = dm.CategoryName,
                    Description = dm.Description,
                    DisplayOrder = dm.DisplayOrder,
                    IsActive = dm.IsActive                  
                });

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sorted device models");
            return null;
        }
    }

    public async Task<DeviceModelDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<DeviceModelDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device model {Id}", id);
            return null;
        }
    }

    public async Task<List<DeviceModelDto>?> GetByBrandAndCategoryAsync(int brandId, int categoryId)
    {
        try
        {
            return await _httpClient.GetAsync<List<DeviceModelDto>>($"{BaseEndpoint}/by-brand-category?brandId={brandId}&categoryId={categoryId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device models for brand {BrandId} and category {CategoryId}", brandId, categoryId);
            return null;
        }
    }

    public async Task<DeviceModelDto?> CreateAsync(CreateDeviceModelRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<CreateDeviceModelRequestDto, DeviceModelDto>(BaseEndpoint, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device model");
            return null;
        }
    }

    public async Task<DeviceModelDto?> UpdateAsync(int id, UpdateDeviceModelRequestDto request)
    {
        try
        {
            return await _httpClient.PutAsync<UpdateDeviceModelRequestDto, DeviceModelDto>($"{BaseEndpoint}/{id}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device model {Id}", id);
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
            _logger.LogError(ex, "Error deleting device model {Id}", id);
            return false;
        }
    }
}

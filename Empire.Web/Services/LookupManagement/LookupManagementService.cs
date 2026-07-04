using Empire.Web.Constants;
using Empire.Web.DTOs;
using Empire.Web.DTOs.Brand;
using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.InventoryCategory;
using Empire.Web.DTOs.Lookup;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.LookupManagement;

public class LookupManagementService : ILookupManagementService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<LookupManagementService> _logger;

    public LookupManagementService(
        IHttpClientService httpClient,
        ILogger<LookupManagementService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<BrandDto>> GetBrandsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync<List<BrandDto>>(RouteConstants.ApiPaths.Brands);
            return response ?? new List<BrandDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands");
            return new List<BrandDto>();
        }
    }

    public async Task<List<DeviceCategoryDto>> GetDeviceCategoriesAsync(int? brandId = null)
    {
        try
        {
            var endpoint = RouteConstants.ApiPaths.DeviceCategories;
            if (brandId.HasValue && brandId.Value > 0)
                endpoint = $"{RouteConstants.ApiPaths.DeviceCategories}/by-brand/{brandId.Value}";

            var response = await _httpClient.GetAsync<List<DeviceCategoryDto>>(endpoint);
            return response ?? new List<DeviceCategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device categories");
            return new List<DeviceCategoryDto>();
        }
    }

    public async Task<List<DeviceModelDto>> GetDeviceModelsAsync(int? categoryId = null, int? brandId = null)
    {
        try
        {
            string endpoint;

            if (brandId.HasValue && brandId.Value > 0 && categoryId.HasValue && categoryId.Value > 0)
            {
                // Use the by-brand-category route when both filters are provided
                endpoint = $"{RouteConstants.ApiPaths.DeviceModels}/by-brand-category?brandId={brandId.Value}&categoryId={categoryId.Value}";
            }
            else
            {
                var queryParams = new List<string>();
                if (categoryId.HasValue && categoryId.Value > 0)
                    queryParams.Add($"categoryId={categoryId.Value}");
                if (brandId.HasValue && brandId.Value > 0)
                    queryParams.Add($"brandId={brandId.Value}");

                endpoint = RouteConstants.ApiPaths.DeviceModels;
                if (queryParams.Any())
                    endpoint += $"?{string.Join("&", queryParams)}";
            }

            var response = await _httpClient.GetAsync<List<DeviceModelDto>>(endpoint);
            return response ?? new List<DeviceModelDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device models");
            return new List<DeviceModelDto>();
        }
    }

    public async Task<List<InventoryCategoryDto>> GetInventoryCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync<List<InventoryCategoryDto>>(
                $"{RouteConstants.ApiPaths.Inventory}/categories");
            return response ?? new List<InventoryCategoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory categories");
            return new List<InventoryCategoryDto>();
        }
    }

    /// <summary>
    /// Returns lookup values for the given category (e.g. "PaymentStatus", "RepairStatus").
    /// Maps to GET /api/lookups/category/{category}
    /// </summary>
    public async Task<List<LookupValueDto>> GetLookupValuesByCategoryAsync(string category)
    {
        try
        {
            var response = await _httpClient.GetAsync<List<LookupValueDto>>(
                string.Format(RouteConstants.ApiEndpoints.GetLookupsByCategory, Uri.EscapeDataString(category)));
            return response ?? new List<LookupValueDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup values for category {Category}", category);
            return new List<LookupValueDto>();
        }
    }

    /// <summary>
    /// Returns lookup values by type — treated as a category alias.
    /// Maps to GET /api/lookups/category/{type}
    /// </summary>
    public async Task<List<LookupValueDto>> GetLookupValuesByTypeAsync(string type)
    {
        try
        {
            var response = await _httpClient.GetAsync<List<LookupValueDto>>(
                string.Format(RouteConstants.ApiEndpoints.GetLookupsByCategory, Uri.EscapeDataString(type)));
            return response ?? new List<LookupValueDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup values for type {Type}", type);
            return new List<LookupValueDto>();
        }
    }

    /// <summary>
    /// Returns all available US states.
    /// Maps to GET /api/lookups/states
    /// </summary>
    public async Task<List<string>> GetStatesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync<List<string>>(
                $"{RouteConstants.ApiPaths.Lookups}/states");
            return response ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting states");
            return new List<string>();
        }
    }
}

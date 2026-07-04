
using AutoMapper;
using Empire.Web.Constants;
using Empire.Web.DTOs.Device;
using Empire.Web.Services.Device;

namespace Empire.Web.Services;

/// <summary>
/// Implementation of Device API service for HTTP communication with backend
/// </summary>
public class DeviceApiService : BaseApiService, IDeviceApiService
{
    private readonly ILogger<DeviceApiService> _typedLogger;
    private readonly IMapper _mapper;

    public DeviceApiService(
        HttpClient httpClient,
        ILogger<DeviceApiService> logger,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper)
        : base(httpClient, logger, httpContextAccessor)
    {
        _typedLogger = logger;
        _mapper = mapper;
    }

    public async Task<IEnumerable<DeviceSelectionDto>> GetByShopAsync(int shopId)
    {
        try
        {
            _typedLogger.LogInformation("Getting devices for shop {ShopId}", shopId);
            // The API reads shopId from the JWT claim — no shopId in the URL
            var result = await GetAsync<IEnumerable<DeviceSelectionDto>>(
                RouteConstants.ApiPaths.Devices);
            return result ?? Enumerable.Empty<DeviceSelectionDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting devices for shop {ShopId}", shopId);
            throw;
        }
    }

    public async Task<IEnumerable<DeviceSelectionDto>> GetDevicesAsync(int shopId)
    {
        return await GetByShopAsync(shopId);
    }

    public async Task<DeviceSelectionDto?> GetByIdAsync(int deviceId)
    {
        try
        {
            _typedLogger.LogInformation("Getting device {DeviceId}", deviceId);
            return await GetAsync<DeviceSelectionDto>(
                $"{RouteConstants.ApiPaths.Devices}/{deviceId}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting device {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task<DeviceDto?> CreateAsync(CreateDeviceRequestDto request)
    {
        try
        {
            _typedLogger.LogInformation("Creating new device");
            return await PostAsync<CreateDeviceRequestDto, DeviceDto>(
                RouteConstants.ApiPaths.Devices, request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error creating device");
            throw;
        }
    }

    public async Task<DeviceDto?> UpdateAsync(int deviceId, UpdateDeviceRequest request)
    {
        try
        {
            _typedLogger.LogInformation("Updating device {DeviceId}", deviceId);
            return await PutAsync<UpdateDeviceRequest, DeviceDto>(
                $"{RouteConstants.ApiPaths.Devices}/{deviceId}", request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error updating device {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int deviceId, int shopId)
    {
        try
        {
            _typedLogger.LogInformation("Deleting device {DeviceId}", deviceId);
            return await DeleteAsync(
                // API enforces shop ownership via JWT
                $"{RouteConstants.ApiPaths.Devices}/{deviceId}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error deleting device {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task<IEnumerable<DeviceDto>> SearchDevicesAsync(string searchTerm, int shopId)
    {
        try
        {
            _typedLogger.LogInformation("Searching devices with term: {SearchTerm}", searchTerm);
            var result = await GetAsync<IEnumerable<DeviceDto>>(
                // API reads shopId from JWT
                $"{RouteConstants.ApiPaths.Devices}/search?term={Uri.EscapeDataString(searchTerm ?? string.Empty)}");
            return result ?? Enumerable.Empty<DeviceDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error searching devices");
            throw;
        }
    }

    public async Task<DeviceDto?> ToggleAvailabilityAsync(int deviceId)
    {
        try
        {
            _typedLogger.LogInformation("Toggling availability for device {DeviceId}", deviceId);
            
            // Get current device
            var device = await GetByIdAsync(deviceId);
            if (device == null)
            {
                _typedLogger.LogWarning("Device {DeviceId} not found", deviceId);
                return null;
            }

            // Map device to update request using AutoMapper
            var updateRequest = _mapper.Map<UpdateDeviceRequest>(device);
            
            // Toggle the availability status
            updateRequest.IsAvailableForSale = !device.IsAvailableForSale;

            return await UpdateAsync(deviceId, updateRequest);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error toggling availability for device {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task<DeviceDto?> MarkAsSoldAsync(int deviceId, int customerId, decimal salePrice)
    {
        try
        {
            _typedLogger.LogInformation("Marking device {DeviceId} as sold to customer {CustomerId}", deviceId, customerId);
            
            // Get current device
            var device = await GetByIdAsync(deviceId);
            if (device == null)
            {
                _typedLogger.LogWarning("Device {DeviceId} not found", deviceId);
                return null;
            }

            // Map device to update request using AutoMapper
            var updateRequest = _mapper.Map<UpdateDeviceRequest>(device);
            
            // Update fields for sold status
            updateRequest.SellingPrice = salePrice;
            updateRequest.IsAvailableForSale = false;
            updateRequest.IsSold = true;
            updateRequest.SoldToCustomerId = customerId;

            return await UpdateAsync(deviceId, updateRequest);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error marking device {DeviceId} as sold", deviceId);
            throw;
        }
    }
}

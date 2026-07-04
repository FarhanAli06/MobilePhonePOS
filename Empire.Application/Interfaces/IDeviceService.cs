using Empire.Application.DTOs.Device;

namespace Empire.Application.Interfaces;

public interface IDeviceService
{
    Task<DeviceHierarchyDto> GetDeviceHierarchyAsync();
    Task<IEnumerable<string>> GetBrandsAsync();
    Task<IEnumerable<string>> GetCategoriesByBrandAsync(string brand);
    Task<IEnumerable<DeviceSelectionDto>> GetModelsByBrandAndCategoryAsync(string brand, string category);
    Task<DeviceSelectionDto?> GetDeviceByIdAsync(int deviceId);
    Task<DeviceSelectionDto> CreateDeviceAsync(string brand, string category, string model, string? modelNumber = null, int? year = null);
    
    // New comprehensive device management methods
    Task<DeviceSelectionDto> CreateDeviceAsync(CreateDeviceRequest request, int userId);
    Task<IEnumerable<DeviceSelectionDto>> GetDevicesAsync(DeviceFilterRequest filter);
    Task<IEnumerable<DeviceSelectionDto>> GetDevicesByShopAsync(int shopId);
    Task<IEnumerable<DeviceSelectionDto>> GetAvailableDevicesForSaleAsync(int shopId);
    Task<IEnumerable<DeviceSelectionDto>> GetSoldDevicesAsync(int shopId);
    Task<DeviceSelectionDto> UpdateDeviceAsync(int id, UpdateDeviceRequest request, int userId);
    Task<bool> DeleteDeviceAsync(int id, int userId);
    Task<bool> MarkDeviceAsSoldAsync(int deviceId, int customerId, decimal? salePrice, int userId);
}


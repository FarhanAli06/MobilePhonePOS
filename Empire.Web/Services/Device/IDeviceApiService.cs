using Empire.Web.DTOs.Device;

namespace Empire.Web.Services.Device
{
    /// <summary>
    /// Interface for Device API service operations
    /// </summary>
    public interface IDeviceApiService
    {
        Task<DeviceSelectionDto?> GetByIdAsync(int id);
        Task<DeviceDto?> CreateAsync(CreateDeviceRequestDto request);
        Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceRequest request);
        Task<IEnumerable<DeviceSelectionDto>> GetByShopAsync(int shopId);
        Task<IEnumerable<DeviceSelectionDto>> GetDevicesAsync(int shopId);
        Task<bool> DeleteAsync(int deviceId, int shopId);
        
        // Business logic methods
        Task<DeviceDto?> ToggleAvailabilityAsync(int deviceId);
        Task<DeviceDto?> MarkAsSoldAsync(int deviceId, int customerId, decimal salePrice);
    }
}

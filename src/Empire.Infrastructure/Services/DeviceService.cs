using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Device;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Empire.Domain.Enums;

namespace Empire.Infrastructure.Services;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ICustomerRepository _customerRepository;

    public DeviceService(IDeviceRepository deviceRepository, ICustomerRepository customerRepository)
    {
        _deviceRepository = deviceRepository;
        _customerRepository = customerRepository;
    }

    public async Task<DeviceSelectionDto> CreateDeviceAsync(CreateDeviceRequest request, int userId)
    {
        var device = new Device
        {
            ShopId = request.ShopId,
            BrandId = request.BrandId, // Updated to use lookup ID
            DeviceCategoryId = request.DeviceCategoryId, // Updated to use lookup ID
            DeviceModelId = request.DeviceModelId, // Updated to use lookup ID
            IMEISerialNumber = request.IMEISerialNumber,
            BatteryHealthPercentage = request.BatteryHealthPercentage,
            NetworkStatus = request.NetworkStatus,
            ScratchesCondition = request.ScratchesCondition,
            BuyingPrice = request.BuyingPrice,
            SellingPrice = request.SellingPrice,
            Source = request.Source,
            Notes = request.Notes,
            IsAvailableForSale = request.IsAvailableForSale,
            IsSold = false,
            CreatedDate = DateTime.UtcNow
        };

        var createdDevice = await _deviceRepository.AddAsync(device);
        await _deviceRepository.SaveChangesAsync();
        return MapToDeviceSelectionDto(createdDevice);
    }

    public async Task<DeviceSelectionDto?> GetDeviceByIdAsync(int id)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        return device != null ? MapToDeviceSelectionDto(device) : null;
    }

    public Task<IEnumerable<DeviceSelectionDto>> GetDevicesAsync(DeviceFilterRequest filter)
    {
        // Note: This method needs to be updated for new lookup structure
        // For now, return empty list
        return Task.FromResult<IEnumerable<DeviceSelectionDto>>(new List<DeviceSelectionDto>());
    }

    public async Task<IEnumerable<DeviceSelectionDto>> GetDevicesByShopAsync(int shopId)
    {
        var devices = await _deviceRepository.GetDevicesByShopAsync(shopId);
        return devices.Select(MapToDeviceSelectionDto);
    }

    public Task<IEnumerable<DeviceSelectionDto>> GetAvailableDevicesForSaleAsync(int shopId)
    {
        var filter = new DeviceFilterRequest
        {
            ShopId = shopId,
            IsAvailableForSale = true,
            IsSold = false
        };
        // Note: This method needs to be updated for new lookup structure
        // For now, return empty list
        return Task.FromResult<IEnumerable<DeviceSelectionDto>>(new List<DeviceSelectionDto>());
    }

    public Task<IEnumerable<DeviceSelectionDto>> GetSoldDevicesAsync(int shopId)
    {
        var filter = new DeviceFilterRequest
        {
            ShopId = shopId,
            IsSold = true
        };
        // Note: This method needs to be updated for new lookup structure
        // For now, return empty list
        return Task.FromResult<IEnumerable<DeviceSelectionDto>>(new List<DeviceSelectionDto>());
    }

    public async Task<DeviceSelectionDto> UpdateDeviceAsync(int id, UpdateDeviceRequest request, int userId)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        if (device == null)
            throw new ArgumentException("Device not found");

        // Note: Brand, Category, Model updates would require lookup table IDs
        // For now, only update the properties that still exist
        device.IMEISerialNumber = request.IMEISerialNumber;
        device.BatteryHealthPercentage = request.BatteryHealthPercentage;
        device.NetworkStatus = request.NetworkStatus;
        device.ScratchesCondition = request.ScratchesCondition;
        device.BuyingPrice = request.BuyingPrice;
        device.SellingPrice = request.SellingPrice;
        device.Source = request.Source;
        device.Notes = request.Notes;
        device.IsAvailableForSale = request.IsAvailableForSale;
        device.IsSold = request.IsSold;
        device.SoldToCustomerId = request.SoldToCustomerId;
        device.ModifiedDate = DateTime.UtcNow;

        if (request.IsSold && !device.IsSold)
        {
            device.SoldDate = DateTime.UtcNow;
        }
        else if (!request.IsSold && device.IsSold)
        {
            device.SoldDate = null;
            device.SoldToCustomerId = null;
        }

        await _deviceRepository.UpdateAsync(device);
        await _deviceRepository.SaveChangesAsync();
        return MapToDeviceSelectionDto(device);
    }

    public async Task<bool> DeleteDeviceAsync(int id, int userId)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        if (device == null)
            return false;

        await _deviceRepository.DeleteAsync(device);
        await _deviceRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkDeviceAsSoldAsync(int deviceId, int customerId, decimal? salePrice, int userId)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId);
        if (device == null || device.IsSold)
            return false;

        device.IsSold = true;
        device.SoldDate = DateTime.UtcNow;
        device.SoldToCustomerId = customerId;
        device.IsAvailableForSale = false;
        device.ModifiedDate = DateTime.UtcNow;

        if (salePrice.HasValue)
        {
            device.SellingPrice = salePrice.Value;
        }

        await _deviceRepository.UpdateAsync(device);
        await _deviceRepository.SaveChangesAsync();
        return true;
    }

    public async Task<DeviceHierarchyDto> GetDeviceHierarchyAsync()
    {
        var brands = await _deviceRepository.GetBrandsAsync();
        var hierarchy = new DeviceHierarchyDto
        {
            Brands = brands.ToList()
        };

        foreach (var brand in brands)
        {
            var categories = await _deviceRepository.GetCategoriesByBrandAsync(brand);
            hierarchy.CategoriesByBrand[brand] = categories.ToList();

            foreach (var category in categories)
            {
                var models = await _deviceRepository.GetModelsByBrandAndCategoryAsync(brand, category);
                var key = $"{brand}_{category}";
                hierarchy.ModelsByBrandCategory[key] = models.Select(MapToDeviceSelectionDto).ToList();
            }
        }

        return hierarchy;
    }

    public async Task<IEnumerable<string>> GetBrandsAsync()
    {
        return await _deviceRepository.GetBrandsAsync();
    }

    public async Task<IEnumerable<string>> GetCategoriesByBrandAsync(string brand)
    {
        return await _deviceRepository.GetCategoriesByBrandAsync(brand);
    }

    public async Task<IEnumerable<DeviceSelectionDto>> GetModelsByBrandAndCategoryAsync(string brand, string category)
    {
        var devices = await _deviceRepository.GetModelsByBrandAndCategoryAsync(brand, category);
        return devices.Select(MapToDeviceSelectionDto);
    }

    public async Task<DeviceSelectionDto> CreateDeviceAsync(string brand, string category, string model, string? modelNumber = null, int? year = null)
    {
        // Check if device already exists
        var existingDevice = await _deviceRepository.GetByBrandCategoryModelAsync(brand, category, model);
        if (existingDevice != null)
            return MapToDeviceSelectionDto(existingDevice);

        // Note: This method needs to be updated to use lookup table IDs
        // For now, create a basic device with minimal properties
        var device = new Device
        {
            ShopId = 1, // Default shop ID - should be passed as parameter
            BrandId = 1, // Default brand ID - should be looked up
            DeviceCategoryId = 1, // Default category ID - should be looked up  
            DeviceModelId = 1, // Default model ID - should be looked up
            IMEISerialNumber = modelNumber ?? string.Empty
        };

        await _deviceRepository.AddAsync(device);
        await _deviceRepository.SaveChangesAsync();

        return MapToDeviceSelectionDto(device);
    }

    private DeviceSelectionDto MapToDeviceSelectionDto(Device device)
    {
        return new DeviceSelectionDto
        {
            Id = device.Id,
            ShopId = device.ShopId,
            Brand = device.Brand?.Name ?? "",
            Category = device.DeviceCategory?.Name ?? "",
            Model = device.DeviceModel?.Name ?? "",
            ModelNumber = device.DeviceModel?.ModelNumber ?? "",
            Year = device.DeviceModel?.Year,
            DeviceType = DeviceType.Phone, // Default for now, will be determined by category
            IMEISerialNumber = device.IMEISerialNumber,
            BatteryHealthPercentage = device.BatteryHealthPercentage,
            NetworkStatus = device.NetworkStatus,
            ScratchesCondition = device.ScratchesCondition,
            BuyingPrice = device.BuyingPrice,
            SellingPrice = device.SellingPrice,
            Source = device.Source,
            Notes = device.Notes,
            IsAvailableForSale = device.IsAvailableForSale,
            IsSold = device.IsSold,
            SoldDate = device.SoldDate,
            SoldToCustomerId = device.SoldToCustomerId,
            SoldToCustomerName = device.SoldToCustomer?.FirstName + " " + device.SoldToCustomer?.LastName ?? "",
            SoldToCustomer = device.SoldToCustomer != null ? new Empire.Application.DTOs.Customer.CustomerDto
            {
                Id = device.SoldToCustomer.Id,
                FirstName = device.SoldToCustomer.FirstName,
                LastName = device.SoldToCustomer.LastName,
                Phone = device.SoldToCustomer.Phone,
                Email = device.SoldToCustomer.Email,
                Address = device.SoldToCustomer.Address,
                City = device.SoldToCustomer.City,
                State = device.SoldToCustomer.State,
                ZipCode = device.SoldToCustomer.ZipCode,
                CreatedDate = device.SoldToCustomer.CreatedDate,
                ModifiedDate = device.SoldToCustomer.ModifiedDate
            } : null,
            CreatedDate = device.CreatedDate,
            ModifiedDate = device.ModifiedDate
        };
    }
}


using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Device;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Empire.Domain.Enums;
using Empire.Infrastructure.Data;
using System.Text.Json;

namespace Empire.Infrastructure.Services;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly EmpireDbContext _context;

    public DeviceService(IDeviceRepository deviceRepository, ICustomerRepository customerRepository, EmpireDbContext context)
    {
        _deviceRepository = deviceRepository;
        _customerRepository = customerRepository;
        _context = context;
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
            GB = request.GB,
            Color = request.Color?.Trim() ?? string.Empty,
            Carrier = request.Carrier?.Trim() ?? string.Empty,
            BatteryHealthPercentage = request.BatteryHealthPercentage,
            NetworkStatus = request.NetworkStatus,
            ScratchesCondition = request.ScratchesCondition,
            BuyingPrice = request.BuyingPrice,
            SellingPrice = request.SellingPrice,
            Source = request.Source,
            Notes = request.Notes,
            IsAvailableForSale = request.IsAvailableForSale,
            IsSold = false,
            LifecycleStatus = "Available",
            CreatedDate = DateTime.UtcNow
        };

        var createdDevice = await _deviceRepository.AddAsync(device);
        await _deviceRepository.SaveChangesAsync();
        _context.DeviceActivities.Add(new DeviceActivity { DeviceId = createdDevice.Id, Action = "Created", Description = "Device added to inventory", NewValues = DeviceSnapshot(createdDevice), PerformedByUserId = userId, PerformedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        return MapToDeviceSelectionDto(createdDevice);
    }

    public async Task<DeviceSelectionDto?> GetDeviceByIdAsync(int id)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        return device != null ? MapToDeviceSelectionDto(device) : null;
    }

    public async Task<IEnumerable<DeviceSelectionDto>> GetDevicesAsync(DeviceFilterRequest filter)
    {
        // Get all devices for the shop first
        if (!filter.ShopId.HasValue)
            return Enumerable.Empty<DeviceSelectionDto>();
            
        var devices = await _deviceRepository.GetDevicesByShopAsync(filter.ShopId.Value);
        
        // Apply filters
        var query = devices.AsEnumerable();
        
        if (filter.IsAvailableForSale.HasValue)
        {
            query = query.Where(d => d.IsAvailableForSale == filter.IsAvailableForSale.Value);
        }
        
        if (filter.IsSold.HasValue)
        {
            query = query.Where(d => d.IsSold == filter.IsSold.Value);
        }
        
        if (filter.BrandId.HasValue)
        {
            query = query.Where(d => d.BrandId == filter.BrandId.Value);
        }
        
        if (filter.DeviceCategoryId.HasValue)
        {
            query = query.Where(d => d.DeviceCategoryId == filter.DeviceCategoryId.Value);
        }
        
        if (filter.DeviceModelId.HasValue)
        {
            query = query.Where(d => d.DeviceModelId == filter.DeviceModelId.Value);
        }
        
        if (!string.IsNullOrEmpty(filter.NetworkStatus))
        {
            query = query.Where(d => d.NetworkStatus == filter.NetworkStatus);
        }
        
        if (!string.IsNullOrEmpty(filter.ScratchesCondition))
        {
            query = query.Where(d => d.ScratchesCondition == filter.ScratchesCondition);
        }
        
        return query.Select(MapToDeviceSelectionDto);
    }

    public async Task<IEnumerable<DeviceSelectionDto>> GetDevicesByShopAsync(int shopId)
    {
        var devices = await _deviceRepository.GetDevicesByShopAsync(shopId);
        return devices.Select(MapToDeviceSelectionDto);
    }

    public async Task<IEnumerable<DeviceSelectionDto>> GetAvailableDevicesForSaleAsync(int shopId)
    {
        var filter = new DeviceFilterRequest
        {
            ShopId = shopId,
            IsAvailableForSale = true,
            IsSold = false
        };
        return await GetDevicesAsync(filter);
    }

    public async Task<IEnumerable<DeviceSelectionDto>> GetSoldDevicesAsync(int shopId)
    {
        var filter = new DeviceFilterRequest
        {
            ShopId = shopId,
            IsSold = true
        };
        return await GetDevicesAsync(filter);
    }

    public async Task<DeviceSelectionDto> UpdateDeviceAsync(int id, UpdateDeviceRequest request, int userId)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        if (device == null)
            throw new ArgumentException("Device not found");

        var oldSnapshot = DeviceSnapshot(device);

        // Update all device properties
        var wasSold = device.IsSold;
        device.BrandId = request.BrandId;
        device.DeviceCategoryId = request.DeviceCategoryId;
        device.DeviceModelId = request.DeviceModelId;
        device.IMEISerialNumber = request.IMEISerialNumber;
        device.GB = request.GB;
        device.Color = request.Color?.Trim() ?? string.Empty;
        device.Carrier = request.Carrier?.Trim() ?? string.Empty;
        device.BatteryHealthPercentage = request.BatteryHealthPercentage;
        device.NetworkStatus = request.NetworkStatus;
        device.ScratchesCondition = request.ScratchesCondition;
        device.BuyingPrice = request.BuyingPrice;
        device.SellingPrice = request.SellingPrice;
        device.Source = request.Source;
        device.Notes = request.Notes;
        device.IsAvailableForSale = request.IsAvailableForSale;
        device.IsSold = request.IsSold;
        device.LifecycleStatus = request.IsSold ? "Sold" : (request.IsAvailableForSale ? "Available" : "NotForSale");
        device.SoldToCustomerId = request.SoldToCustomerId;
        device.ModifiedDate = DateTime.UtcNow;

        if (request.IsSold && !wasSold)
        {
            device.SoldDate = DateTime.UtcNow;
        }
        else if (!request.IsSold && wasSold)
        {
            device.SoldDate = null;
            device.SoldToCustomerId = null;
        }

        await _deviceRepository.UpdateAsync(device);
        await _deviceRepository.SaveChangesAsync();
        var newSnapshot = DeviceSnapshot(device);
        _context.DeviceActivities.Add(new DeviceActivity
        {
            DeviceId = device.Id,
            Action = "Updated",
            Description = BuildDeviceChangeSummary(oldSnapshot, newSnapshot),
            OldValues = oldSnapshot,
            NewValues = newSnapshot,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
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
        device.LifecycleStatus = "Sold";
        device.ModifiedDate = DateTime.UtcNow;

        if (salePrice.HasValue)
        {
            device.SellingPrice = salePrice.Value;
        }

        await _deviceRepository.UpdateAsync(device);
        await _deviceRepository.SaveChangesAsync();
        _context.DeviceActivities.Add(new DeviceActivity { DeviceId = device.Id, Action = "Sold", Description = "Device marked sold", NewValues = DeviceSnapshot(device), PerformedByUserId = userId, PerformedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<DeviceHistoryDto>> GetDeviceHistoryAsync(int deviceId)
    {
        return await _context.DeviceActivities.AsNoTracking()
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.PerformedAt)
            .Select(x => new DeviceHistoryDto { Id = x.Id, Action = x.Action, Description = x.Description, OldValues = x.OldValues, NewValues = x.NewValues, PerformedAt = x.PerformedAt, PerformedBy = x.PerformedByUser == null ? "System" : x.PerformedByUser.FirstName + " " + x.PerformedByUser.LastName })
            .ToListAsync();
    }

    private static string DeviceSnapshot(Device d) => JsonSerializer.Serialize(new { d.IMEISerialNumber, d.GB, d.Color, d.Carrier, d.BuyingPrice, d.SellingPrice, d.IsAvailableForSale, d.IsSold, d.LifecycleStatus, d.SoldToCustomerId });

    private static string BuildDeviceChangeSummary(string oldSnapshot, string newSnapshot)
    {
        using var oldDocument = JsonDocument.Parse(oldSnapshot);
        using var newDocument = JsonDocument.Parse(newSnapshot);
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IMEISerialNumber"] = "IMEI/serial",
            ["GB"] = "Storage",
            ["Color"] = "Color",
            ["Carrier"] = "Carrier",
            ["BuyingPrice"] = "Cost price",
            ["SellingPrice"] = "Selling price",
            ["IsAvailableForSale"] = "Available for sale",
            ["IsSold"] = "Sold",
            ["LifecycleStatus"] = "Status",
            ["SoldToCustomerId"] = "Customer"
        };

        var changes = new List<string>();
        foreach (var property in newDocument.RootElement.EnumerateObject())
        {
            if (!oldDocument.RootElement.TryGetProperty(property.Name, out var oldValue) || oldValue.ToString() == property.Value.ToString())
                continue;

            var label = labels.GetValueOrDefault(property.Name, property.Name);
            changes.Add($"{label}: {FormatHistoryValue(property.Name, oldValue)} → {FormatHistoryValue(property.Name, property.Value)}");
        }

        return changes.Count == 0 ? "No device values changed" : string.Join("; ", changes);
    }

    private static string FormatHistoryValue(string propertyName, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return "Not set";
        if ((propertyName.Equals("BuyingPrice", StringComparison.OrdinalIgnoreCase) || propertyName.Equals("SellingPrice", StringComparison.OrdinalIgnoreCase))
            && value.TryGetDecimal(out var amount))
            return amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return value.GetBoolean() ? "Yes" : "No";
        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text) ? "Not set" : text;
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

    public Task<DeviceSelectionDto> CreateDeviceAsync(string brand, string category, string model, string? modelNumber = null, int? year = null)
    {
        // This legacy overload is not supported — use CreateDeviceAsync(CreateDeviceRequest, int) instead.
        throw new NotSupportedException(
            "CreateDeviceAsync(string, string, string) is not supported. " +
            "Use CreateDeviceAsync(CreateDeviceRequest, int) with a proper shopId and lookup IDs.");
    }

    private DeviceSelectionDto MapToDeviceSelectionDto(Device device)
    {
        return new DeviceSelectionDto
        {
            Id = device.Id,
            ShopId = device.ShopId,
            BrandId = device.BrandId,
            DeviceCategoryId = device.DeviceCategoryId,
            DeviceModelId = device.DeviceModelId,
            Brand = device.Brand?.Name ?? "",
            Category = device.DeviceCategory?.Name ?? "",
            Model = device.DeviceModel?.Name ?? "",
            ModelNumber = device.DeviceModel?.ModelNumber ?? "",
            Year = device.DeviceModel?.Year,
            DeviceType = DeviceType.Phone, // Default for now, will be determined by category
            IMEISerialNumber = device.IMEISerialNumber,
            GB = device.GB,
            Color = device.Color,
            Carrier = device.Carrier,
            BatteryHealthPercentage = device.BatteryHealthPercentage,
            NetworkStatus = device.NetworkStatus,
            ScratchesCondition = device.ScratchesCondition,
            BuyingPrice = device.BuyingPrice,
            SellingPrice = device.SellingPrice,
            Source = device.Source,
            Notes = device.Notes,
            IsAvailableForSale = device.IsAvailableForSale,
            IsSold = device.IsSold,
            LifecycleStatus = device.LifecycleStatus,
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


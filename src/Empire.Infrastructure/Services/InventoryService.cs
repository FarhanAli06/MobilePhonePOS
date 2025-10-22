using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Inventory;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Enums;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IShopRepository _shopRepository;
    private readonly EmpireDbContext _context;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IDeviceRepository deviceRepository,
        IShopRepository shopRepository,
        EmpireDbContext context)
    {
        _inventoryRepository = inventoryRepository;
        _deviceRepository = deviceRepository;
        _shopRepository = shopRepository;
        _context = context;
    }

    public async Task<InventoryDto> CreateInventoryAsync(CreateInventoryRequest request)
    {
        // Validate device if provided
        if (request.DeviceId.HasValue)
        {
            var device = await _deviceRepository.GetByIdAsync(request.DeviceId.Value);
            if (device == null)
                throw new ArgumentException("Device not found");
        }

        var inventory = new Inventory
        {
            ShopId = request.ShopId,
            DeviceId = request.DeviceId,
            Name = request.Name,
            DeviceType = request.DeviceType,
            Category = request.Category,
            Stock = request.Stock,
            ReorderPoint = request.ReorderPoint,
            CostPrice = request.CostPrice,
            RetailPrice = request.RetailPrice,
            LowStockNotification = request.LowStockNotification
        };

        await _inventoryRepository.AddAsync(inventory);
        await _inventoryRepository.SaveChangesAsync();

        return await MapToInventoryDto(inventory);
    }

    public async Task<InventoryDto?> GetInventoryByIdAsync(int inventoryId, int shopId)
    {
        var inventory = await _context.Inventories
            .Include(i => i.Device)
            .Include(i => i.Shop)
            .FirstOrDefaultAsync(i => i.Id == inventoryId && i.ShopId == shopId && !i.IsDeleted);

        return inventory != null ? await MapToInventoryDto(inventory) : null;
    }

    public async Task<IEnumerable<InventoryDto>> GetInventoryAsync(InventoryFilterRequest filter)
    {
        var query = _context.Inventories
            .Include(i => i.Device)
            .Include(i => i.Shop)
            .Where(i => i.ShopId == filter.ShopId && !i.IsDeleted);

        // Apply device type filter
        if (filter.DeviceType.HasValue)
            query = query.Where(i => i.DeviceType == filter.DeviceType.Value);

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(i => i.Category == filter.Category);

        // Apply low stock filter
        if (filter.LowStockOnly == true)
            query = query.Where(i => i.Stock <= i.ReorderPoint);

        // Apply low stock notification filter
        if (filter.LowStockNotificationOnly == true)
            query = query.Where(i => i.Stock <= i.ReorderPoint && i.LowStockNotification);

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(i => 
                i.Name.ToLower().Contains(searchTerm) ||
                i.Category.ToLower().Contains(searchTerm) ||
                (i.Device != null && (
                    (i.Device.Brand != null && i.Device.Brand.Name.ToLower().Contains(searchTerm)) ||
                    (i.Device.DeviceModel != null && i.Device.DeviceModel.Name.ToLower().Contains(searchTerm)) ||
                    (i.Device.DeviceCategory != null && i.Device.DeviceCategory.Name.ToLower().Contains(searchTerm))
                )));
        }

        var inventories = await query
            .OrderBy(i => i.Name)
            .ToListAsync();

        var inventoryDtos = new List<InventoryDto>();
        foreach (var inventory in inventories)
        {
            inventoryDtos.Add(await MapToInventoryDto(inventory));
        }

        return inventoryDtos;
    }

    public async Task<InventoryDto?> UpdateInventoryAsync(int inventoryId, UpdateInventoryRequest request)
    {
        var inventory = await _inventoryRepository.GetByIdAsync(inventoryId);
        if (inventory == null)
            return null;

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(request.Name))
            inventory.Name = request.Name;

        if (request.DeviceType.HasValue)
            inventory.DeviceType = request.DeviceType.Value;

        if (!string.IsNullOrWhiteSpace(request.Category))
            inventory.Category = request.Category;

        if (request.Stock.HasValue)
            inventory.Stock = request.Stock.Value;

        if (request.ReorderPoint.HasValue)
            inventory.ReorderPoint = request.ReorderPoint.Value;

        if (request.CostPrice.HasValue)
            inventory.CostPrice = request.CostPrice.Value;

        if (request.RetailPrice.HasValue)
            inventory.RetailPrice = request.RetailPrice.Value;

        if (request.LowStockNotification.HasValue)
            inventory.LowStockNotification = request.LowStockNotification.Value;

        await _inventoryRepository.UpdateAsync(inventory);
        await _inventoryRepository.SaveChangesAsync();

        return await MapToInventoryDto(inventory);
    }

    public async Task<bool> DeleteInventoryAsync(int inventoryId, int shopId)
    {
        var inventory = await _inventoryRepository.FirstOrDefaultAsync(i => i.Id == inventoryId && i.ShopId == shopId);
        if (inventory == null)
            return false;

        await _inventoryRepository.DeleteAsync(inventory);
        await _inventoryRepository.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<InventoryDto>> GetLowStockItemsAsync(int shopId)
    {
        var inventories = await _inventoryRepository.GetLowStockItemsWithNotificationAsync(shopId);
        var inventoryDtos = new List<InventoryDto>();
        foreach (var inventory in inventories)
        {
            inventoryDtos.Add(await MapToInventoryDto(inventory));
        }
        return inventoryDtos;
    }

    public async Task<bool> AdjustInventoryAsync(InventoryAdjustmentRequest request, int userId)
    {
        var inventory = await _inventoryRepository.GetByIdAsync(request.InventoryId);
        if (inventory == null)
            return false;

        // Create adjustment record
        var adjustment = new InventoryAdjustment
        {
            InventoryId = request.InventoryId,
            UserId = userId,
            AdjustmentType = request.AdjustmentType,
            Quantity = request.Quantity,
            Reason = request.Reason,
            AdjustmentDate = DateTime.UtcNow
        };

        // Update inventory stock
        inventory.Stock += request.Quantity;
        if (inventory.Stock < 0)
            inventory.Stock = 0; // Prevent negative stock

        await _context.InventoryAdjustments.AddAsync(adjustment);
        await _inventoryRepository.UpdateAsync(inventory);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<InventoryDto>> GetInventoryByDeviceTypeAsync(int shopId, DeviceType deviceType)
    {
        var inventories = await _inventoryRepository.GetInventoryByDeviceTypeAsync(shopId, deviceType);
        var inventoryDtos = new List<InventoryDto>();
        foreach (var inventory in inventories)
        {
            inventoryDtos.Add(await MapToInventoryDto(inventory));
        }
        return inventoryDtos;
    }

    private async Task<InventoryDto> MapToInventoryDto(Inventory inventory)
    {
        // Ensure all navigation properties are loaded
        if (inventory.Device == null && inventory.DeviceId.HasValue)
            inventory.Device = await _deviceRepository.GetByIdAsync(inventory.DeviceId.Value);
        
        if (inventory.Shop == null)
            inventory.Shop = await _shopRepository.GetByIdAsync(inventory.ShopId) ?? new Shop();

        return new InventoryDto
        {
            Id = inventory.Id,
            ShopId = inventory.ShopId,
            ShopName = inventory.Shop?.Name ?? "",
            DeviceId = inventory.DeviceId,
            DeviceBrand = inventory.Device?.Brand?.Name ?? "",
            DeviceCategory = inventory.Device?.DeviceCategory?.Name ?? "",
            DeviceModel = inventory.Device?.DeviceModel?.Name ?? "",
            Name = inventory.Name,
            DeviceType = inventory.DeviceType,
            Category = inventory.Category,
            Stock = inventory.Stock,
            ReorderPoint = inventory.ReorderPoint,
            CostPrice = inventory.CostPrice,
            RetailPrice = inventory.RetailPrice,
            LowStockNotification = inventory.LowStockNotification,
            CreatedDate = inventory.CreatedDate,
            ModifiedDate = inventory.ModifiedDate
        };
    }
}


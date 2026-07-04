using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Item;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Infrastructure.Data;
namespace Empire.Infrastructure.Services;
public class ItemService : IItemService
{
    private readonly EmpireDbContext _context;
    public ItemService(EmpireDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<ItemDto>> GetAllItemsAsync(int shopId)
    {
        return await _context.Items
            .Where(i => !i.IsDeleted && i.ShopId == shopId)
            .OrderBy(i => i.Name)
            .Select(i => new ItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                IsActive = i.IsActive,
                CreatedDate = i.CreatedDate,
                ModifiedDate = i.ModifiedDate
            })
            .ToListAsync();
    }
    public async Task<ItemDto> GetItemByIdAsync(int id, int shopId)
    {
        var item = await _context.Items
            .Where(i => i.Id == id && !i.IsDeleted && i.ShopId == shopId)
            .FirstOrDefaultAsync();
        if (item == null)
            throw new KeyNotFoundException($"Item with ID {id} not found in this shop.");
        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive,
            CreatedDate = item.CreatedDate,
            ModifiedDate = item.ModifiedDate
        };
    }
    public async Task<ItemDto> CreateItemAsync(CreateItemRequest request, int shopId)
    {
        var exists = await _context.Items
            .AnyAsync(i => i.Name == request.Name && !i.IsDeleted && i.ShopId == shopId);
        if (exists)
            throw new InvalidOperationException("An item with this name already exists in this shop.");
        var item = new Item
        {
            ShopId = shopId,
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive,
            CreatedDate = item.CreatedDate,
            ModifiedDate = item.ModifiedDate
        };
    }
    public async Task UpdateItemAsync(int id, UpdateItemRequest request, int shopId)
    {
        var item = await _context.Items
            .Where(i => i.Id == id && !i.IsDeleted && i.ShopId == shopId)
            .FirstOrDefaultAsync();
        if (item == null)
            throw new KeyNotFoundException("Item not found in this shop.");
        var exists = await _context.Items
            .AnyAsync(i => i.Name == request.Name && i.Id != id && !i.IsDeleted && i.ShopId == shopId);
        if (exists)
            throw new InvalidOperationException("An item with this name already exists in this shop.");
        item.Name = request.Name;
        item.Description = request.Description;
        item.IsActive = request.IsActive;
        item.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
    public async Task DeleteItemAsync(int id, int shopId)
    {
        var item = await _context.Items
            .Where(i => i.Id == id && !i.IsDeleted && i.ShopId == shopId)
            .FirstOrDefaultAsync();
        if (item == null)
            throw new KeyNotFoundException("Item not found in this shop.");
        var hasInventory = await _context.InventoryItems
            .AnyAsync(i => i.ItemId == id && !i.IsDeleted);
        if (hasInventory)
            throw new InvalidOperationException("Cannot delete item. It is being used in inventory items.");
        item.IsDeleted = true;
        item.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

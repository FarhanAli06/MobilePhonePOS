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

    public async Task<IEnumerable<ItemDto>> GetAllItemsAsync()
    {
        return await _context.Items
            .Where(i => !i.IsDeleted)
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

    public async Task<ItemDto> GetItemByIdAsync(int id)
    {
        var item = await _context.Items
            .Where(i => i.Id == id && !i.IsDeleted)
            .FirstOrDefaultAsync();

        if (item == null)
        {
            throw new KeyNotFoundException($"Item with ID {id} not found.");
        }

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

    public async Task<ItemDto> CreateItemAsync(CreateItemRequest request)
    {
        // Check if item already exists
        var exists = await _context.Items
            .AnyAsync(i => i.Name == request.Name && !i.IsDeleted);

        if (exists)
        {
            throw new InvalidOperationException("An item with this name already exists.");
        }

        var item = new Item
        {
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

    public async Task UpdateItemAsync(int id, UpdateItemRequest request)
    {
        var item = await _context.Items
            .Where(i => i.Id == id && !i.IsDeleted)
            .FirstOrDefaultAsync();

        if (item == null)
        {
            throw new KeyNotFoundException("Item not found.");
        }

        // Check if name is already used by another item
        var exists = await _context.Items
            .AnyAsync(i => i.Name == request.Name && i.Id != id && !i.IsDeleted);

        if (exists)
        {
            throw new InvalidOperationException("An item with this name already exists.");
        }

        item.Name = request.Name;
        item.Description = request.Description;
        item.IsActive = request.IsActive;
        item.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteItemAsync(int id)
    {
        var item = await _context.Items
            .Where(i => i.Id == id && !i.IsDeleted)
            .FirstOrDefaultAsync();

        if (item == null)
        {
            throw new KeyNotFoundException("Item not found.");
        }

        // Check if item is being used in inventory
        var hasInventory = await _context.InventoryItems
            .AnyAsync(i => i.ItemId == id && !i.IsDeleted);

        if (hasInventory)
        {
            throw new InvalidOperationException("Cannot delete item. It is being used in inventory items.");
        }

        item.IsDeleted = true;
        item.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

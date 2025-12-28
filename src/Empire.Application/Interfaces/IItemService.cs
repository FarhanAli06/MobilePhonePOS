using Empire.Application.DTOs.Item;

namespace Empire.Application.Interfaces;

public interface IItemService
{
    Task<IEnumerable<ItemDto>> GetAllItemsAsync();
    Task<ItemDto> GetItemByIdAsync(int id);
    Task<ItemDto> CreateItemAsync(CreateItemRequest request);
    Task UpdateItemAsync(int id, UpdateItemRequest request);
    Task DeleteItemAsync(int id);
}

using Empire.Application.DTOs.Item;
namespace Empire.Application.Interfaces;
public interface IItemService
{
    Task<IEnumerable<ItemDto>> GetAllItemsAsync(int shopId);
    Task<ItemDto> GetItemByIdAsync(int id, int shopId);
    Task<ItemDto> CreateItemAsync(CreateItemRequest request, int shopId);
    Task UpdateItemAsync(int id, UpdateItemRequest request, int shopId);
    Task DeleteItemAsync(int id, int shopId);
}

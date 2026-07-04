using Empire.Web.DTOs.Item;

namespace Empire.Web.Services.Item
{
    public interface IItemApiService
    {
        Task<List<ItemDto>> GetAllAsync();
        Task<ItemDto?> GetByIdAsync(int id);
        Task<ItemDto?> CreateAsync(CreateItemRequest request);
        Task<ItemDto?> UpdateAsync(int id, UpdateItemRequest request);
        Task<bool> DeleteAsync(int id);
        Task<bool> CheckDuplicateNameAsync(string name, int? excludeId = null);
        Task<List<ItemDto>> GetActiveItemsAsync();
    }
}

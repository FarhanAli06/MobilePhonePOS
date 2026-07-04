using Empire.Web.DTOs.Shop;

namespace Empire.Web.Services.Shop
{
    public interface IShopApiService
    {
        Task<List<ShopDto>> GetAllAsync();
        Task<ShopDto?> GetByIdAsync(int id);
        Task<ShopDto?> CreateAsync(CreateShopRequestDto request);
        Task<ShopDto?> UpdateAsync(int id, UpdateShopRequestDto request);
        Task<bool> DeleteAsync(int id);
        Task<ShopDto?> GetCurrentShopAsync();
        Task<List<ShopDto>> GetShopsByUserAsync(int userId);
        Task<ShopDto> CreateShopAsync(CreateShopRequestDto request, int userId);
        Task<ShopDto?> GetShopByIdAsync(int id);
        Task UpdateShopAsync(int id, UpdateShopRequestDto request, int userId);
        Task DeleteShopAsync(int id);
    }
}

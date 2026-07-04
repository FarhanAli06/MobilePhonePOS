using Empire.Application.DTOs.Shop;

namespace Empire.Application.Interfaces;

public interface IShopService
{
    Task<IEnumerable<ShopDto>> GetAllShopsAsync();
    Task<IEnumerable<ShopDto>> GetShopsByUserAsync(int userId);
    Task<ShopDto?> GetShopByIdAsync(int id);
    Task<ShopDto> CreateShopAsync(CreateShopRequest request, int userId);
    Task<ShopDto> UpdateShopAsync(int id, UpdateShopRequest request, int userId);
    Task DeleteShopAsync(int id);
}


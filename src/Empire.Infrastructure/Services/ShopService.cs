using Empire.Application.DTOs.Shop;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;

namespace Empire.Infrastructure.Services;

public class ShopService : IShopService
{
    private readonly IShopRepository _shopRepository;

    public ShopService(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<IEnumerable<ShopDto>> GetAllShopsAsync()
    {
        var shops = await _shopRepository.GetAllAsync();
        return shops.Select(MapToShopDto);
    }

    public async Task<IEnumerable<ShopDto>> GetShopsByUserAsync(int userId)
    {
        var shops = await _shopRepository.GetShopsByUserAsync(userId);
        return shops.Select(MapToShopDto);
    }

    public async Task<ShopDto?> GetShopByIdAsync(int id)
    {
        var shop = await _shopRepository.GetByIdAsync(id);
        return shop != null ? MapToShopDto(shop) : null;
    }

    public async Task<ShopDto> CreateShopAsync(CreateShopRequest request, int userId)
    {
        var shop = new Shop
        {
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        // Add the shop and save changes to get the ID
        var createdShop = await _shopRepository.AddAsync(shop);
        await _shopRepository.SaveChangesAsync(); // Ensure the shop is saved and has an ID
        
        return MapToShopDto(createdShop);
    }

    public async Task<ShopDto> UpdateShopAsync(int id, UpdateShopRequest request, int userId)
    {
        var shop = await _shopRepository.GetByIdAsync(id);
        if (shop == null)
        {
            throw new ArgumentException($"Shop with ID {id} not found");
        }

        shop.Name = request.Name;
        shop.Address = request.Address;
        shop.City = request.City;
        shop.State = request.State;
        shop.ZipCode = request.ZipCode;
        shop.Phone = request.Phone;
        shop.Email = request.Email;
        shop.ModifiedDate = DateTime.UtcNow;

        await _shopRepository.UpdateAsync(shop);
        return MapToShopDto(shop);
    }

    public async Task DeleteShopAsync(int id)
    {
        var shop = await _shopRepository.GetByIdAsync(id);
        if (shop == null)
        {
            throw new ArgumentException($"Shop with ID {id} not found");
        }

        await _shopRepository.DeleteAsync(shop);
    }

    private static ShopDto MapToShopDto(Shop shop)
    {
        return new ShopDto
        {
            Id = shop.Id,
            Name = shop.Name,
            Address = shop.Address,
            City = shop.City,
            State = shop.State,
            ZipCode = shop.ZipCode,
            Phone = shop.Phone,
            Email = shop.Email,
            LogoPath = shop.LogoPath,
            IsActive = shop.IsActive,
            CreatedDate = shop.CreatedDate,
            ModifiedDate = shop.ModifiedDate
        };
    }
}


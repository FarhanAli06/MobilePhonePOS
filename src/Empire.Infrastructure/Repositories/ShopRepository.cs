using Microsoft.EntityFrameworkCore;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Repositories;

public class ShopRepository : Repository<Shop>, IShopRepository
{
    public ShopRepository(EmpireDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Shop>> GetActiveShopsAsync()
    {
        return await _dbSet
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shop>> GetShopsByUserIdAsync(int userId)
    {
        return await _context.UserShopRoles
            .Where(usr => usr.UserId == userId)
            .Include(usr => usr.Shop)
            .Select(usr => usr.Shop)
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<Shop>> GetShopsByUserAsync(int userId)
    {
        try
        {
            // Use a fresh query to avoid connection issues
            var userShopRoles = await _context.UserShopRoles
                .Where(usr => usr.UserId == userId)
                .Include(usr => usr.Shop)
                .Where(usr => usr.Shop != null && usr.Shop.IsActive)
                .ToListAsync();

            return userShopRoles.Select(usr => usr.Shop).Where(s => s != null).Cast<Shop>();
        }
        catch (Exception ex)
        {
            // Log the error and return empty list to prevent application crash
            // In production, you should use proper logging
            Console.WriteLine($"Error in GetShopsByUserAsync: {ex.Message}");
            return new List<Shop>();
        }
    }
}


using Microsoft.EntityFrameworkCore;
using Empire.Domain.Entities;
using Empire.Domain.Enums;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(EmpireDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.UserShopRoles)
            .ThenInclude(usr => usr.Shop)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.UserShopRoles)
            .ThenInclude(usr => usr.Shop)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _dbSet
            .AnyAsync(u => u.Username == username);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet
            .AnyAsync(u => u.Email == email);
    }

    public override async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _dbSet
            .Include(u => u.UserShopRoles)
            .ThenInclude(usr => usr.Shop)
            .Where(e => !e.IsDeleted)
            .ToListAsync();
    }

    public override async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(u => u.UserShopRoles)
            .ThenInclude(usr => usr.Shop)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<IEnumerable<User>> GetUsersByShopAsync(int shopId)
    {
        return await _context.UserShopRoles
            .Where(usr => usr.ShopId == shopId)
            .Include(usr => usr.User)
            .ThenInclude(u => u.UserShopRoles)
            .ThenInclude(usr => usr.Shop)
            .Select(usr => usr.User)
            .Where(u => u.IsActive)
            .ToListAsync();
    }

    public async Task AddUserShopRoleAsync(UserShopRole userShopRole)
    {
        _context.UserShopRoles.Add(userShopRole);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveUserShopRoleAsync(int userId, int shopId)
    {
        var userShopRole = await _context.UserShopRoles
            .FirstOrDefaultAsync(usr => usr.UserId == userId && usr.ShopId == shopId);
        
        if (userShopRole != null)
        {
            _context.UserShopRoles.Remove(userShopRole);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateUserShopRoleAsync(int userId, int shopId, UserRole role)
    {
        var userShopRole = await _context.UserShopRoles
            .FirstOrDefaultAsync(usr => usr.UserId == userId && usr.ShopId == shopId);
        
        if (userShopRole != null)
        {
            userShopRole.Role = role;
            userShopRole.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}


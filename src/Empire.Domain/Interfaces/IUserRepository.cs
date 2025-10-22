using Empire.Domain.Entities;
using Empire.Domain.Enums;

namespace Empire.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<IEnumerable<User>> GetUsersByShopAsync(int shopId);
    Task AddUserShopRoleAsync(UserShopRole userShopRole);
    Task RemoveUserShopRoleAsync(int userId, int shopId);
    Task UpdateUserShopRoleAsync(int userId, int shopId, UserRole role);
}


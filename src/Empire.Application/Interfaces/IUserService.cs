using Empire.Application.DTOs.Auth;
using Empire.Application.DTOs.User;
using Empire.Domain.Enums;

namespace Empire.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserDto>> GetUsersByShopAsync(int shopId);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<IEnumerable<UserDto>> GetFilteredUsersAsync(UserFilterRequest filter);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, int createdByUserId);
    Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request, int modifiedByUserId);
    Task DeleteUserAsync(int id);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task AssignUserToShopAsync(int userId, int shopId, UserRole role);
    Task RemoveUserFromShopAsync(int userId, int shopId);
    Task UpdateUserRoleInShopAsync(int userId, int shopId, UserRole role);
}


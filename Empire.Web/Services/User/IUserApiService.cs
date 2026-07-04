using Empire.Web.DTOs.User;

namespace Empire.Web.Services.User
{
    public interface IUserApiService
    {
        Task<List<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(int id);
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto?> GetByUsernameAsync(string username);
        Task<List<UserDto>> GetByShopAsync(int shopId);
        Task<UserDto?> CreateAsync(CreateUserRequestDto request);
        Task<UserDto?> UpdateAsync(int id, UpdateUserRequestDto request);
        Task<bool> DeleteAsync(int id);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task AssignUserToShopAsync(int userId, int shopId, string role);
        Task RemoveUserFromShopAsync(int userId, int shopId);
        Task UpdateUserRoleInShopAsync(int userId, int shopId, string role);
    }
}

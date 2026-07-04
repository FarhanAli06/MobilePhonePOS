using Empire.Web.DTOs.User;

namespace Empire.Web.Services.User;

/// <summary>
/// Implementation of User API service — extends BaseApiService so the JWT token
/// is automatically forwarded on every request via SetAuthHeader().
/// </summary>
public class UserApiService : BaseApiService, IUserApiService
{
    private readonly ILogger<UserApiService> _typedLogger;
    private const string BaseEndpoint = "api/users";

    public UserApiService(
        HttpClient httpClient,
        ILogger<UserApiService> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(httpClient, logger, httpContextAccessor)
    {
        _typedLogger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        try
        {
            var result = await GetAsync<List<UserDto>>(BaseEndpoint);
            return result ?? new List<UserDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting all users");
            return new List<UserDto>();
        }
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        try
        {
            return await GetAsync<UserDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting user {Id}", id);
            return null;
        }
    }

    public async Task<UserDto?> GetByUsernameAsync(string username)
    {
        try
        {
            return await GetAsync<UserDto>($"{BaseEndpoint}/username/{username}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting user by username {Username}", username);
            return null;
        }
    }

    public async Task<List<UserDto>> GetByShopAsync(int shopId)
    {
        try
        {
            var result = await GetAsync<List<UserDto>>($"{BaseEndpoint}/shop/{shopId}");
            return result ?? new List<UserDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting users for shop {ShopId}", shopId);
            return new List<UserDto>();
        }
    }

    public async Task<UserDto?> CreateAsync(CreateUserRequestDto request)
    {
        try
        {
            return await PostAsync<CreateUserRequestDto, UserDto>(BaseEndpoint, request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error creating user");
            throw;
        }
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserRequestDto request)
    {
        try
        {
            return await PutAsync<UpdateUserRequestDto, UserDto>($"{BaseEndpoint}/{id}", request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error updating user {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            return await DeleteAsync($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error deleting user {Id}", id);
            return false;
        }
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseEndpoint}/exists/username/{username}");
            if (!response.IsSuccessStatusCode) return false;
            var content = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<bool>(content);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error checking username {Username}", username);
            return false;
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseEndpoint}/exists/email/{email}");
            if (!response.IsSuccessStatusCode) return false;
            var content = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<bool>(content);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error checking email {Email}", email);
            return false;
        }
    }

    public async Task AssignUserToShopAsync(int userId, int shopId, string role)
    {
        try
        {
            var request = new { UserId = userId, ShopId = shopId, Role = role };
            await PostAsync<object, object>($"{BaseEndpoint}/{userId}/assign-shop", request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error assigning user {UserId} to shop {ShopId}", userId, shopId);
            throw;
        }
    }

    public async Task RemoveUserFromShopAsync(int userId, int shopId)
    {
        try
        {
            await DeleteAsync($"{BaseEndpoint}/{userId}/shop/{shopId}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error removing user {UserId} from shop {ShopId}", userId, shopId);
            throw;
        }
    }

    public async Task UpdateUserRoleInShopAsync(int userId, int shopId, string role)
    {
        try
        {
            var request = new { Role = role };
            await PutAsync<object, object>($"{BaseEndpoint}/{userId}/shop/{shopId}/role", request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error updating role for user {UserId} in shop {ShopId}", userId, shopId);
            throw;
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(int id) => await GetByIdAsync(id);
}

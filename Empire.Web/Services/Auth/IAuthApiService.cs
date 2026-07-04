using Empire.Web.DTOs.Auth;

namespace Empire.Web.Services.Auth
{
    /// <summary>
    /// Interface for Authentication API service operations
    /// </summary>
    public interface IAuthApiService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<bool> LogoutAsync();
        Task<bool> ValidateTokenAsync(string token);
        Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<Empire.Web.DTOs.User.UserDto?> GetCurrentUserAsync();
    }
}

using Empire.Web.DTOs.Auth;
using Empire.Web.Services.Auth;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.API;

/// <summary>
/// API service for Authentication operations
/// </summary>
public class AuthApiService : IAuthApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<AuthApiService> _logger;
    private const string BaseEndpoint = "/api/auth";

    public AuthApiService(
        IHttpClientService httpClient,
        ILogger<AuthApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<LoginRequestDto, LoginResponseDto>($"{BaseEndpoint}/login", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return null;
        }
    }

    public async Task<LoginResponseDto?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            return await _httpClient.PostAsync<RegisterRequest, LoginResponseDto>($"{BaseEndpoint}/register", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return null;
        }
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<RefreshTokenRequestDto, LoginResponseDto>($"{BaseEndpoint}/refresh", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return null;
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            var result = await _httpClient.PostAsync<object, object>($"{BaseEndpoint}/logout", new { });
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return false;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var result = await _httpClient.PostAsync<object, object>($"{BaseEndpoint}/validate", new { Token = token });
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    public async Task<Empire.Web.DTOs.User.UserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _httpClient.GetAsync<Empire.Web.DTOs.User.UserDto>($"{BaseEndpoint}/current-user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }
}

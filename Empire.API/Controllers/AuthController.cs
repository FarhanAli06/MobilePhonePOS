using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Auth;

namespace Empire.API.Controllers;

/// <summary>
/// Authentication and authorization endpoints
/// </summary>
[AllowAnonymous]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IWebHostEnvironment env)
    {
        _authService = authService;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// User login
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var result = await _authService.LoginAsync(request);

            if (result == null)
                return UnauthorizedResponse("Invalid username or password");

            // Set JWT token in HttpOnly cookie for security.
            // Secure=true is required in production (HTTPS). In development the app
            // runs on HTTP, so Secure must be false or browsers will not send the cookie.
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(), // false in dev (HTTP), true in prod (HTTPS)
                SameSite = SameSiteMode.Lax,    // Lax allows same-site HTTP redirects
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("AuthToken", result.AccessToken, cookieOptions);

            _logger.LogInformation("User {Username} logged in successfully", request.Username);

            // Return the full login response including AccessToken so the Web layer
            // (LoginResponseDto) can deserialize it correctly.  The token is also set
            // in an HttpOnly cookie for browser-based requests.
            var defaultShop = result.ShopRoles.FirstOrDefault();
            var response = new
            {
                // Top-level fields expected by Web LoginResponseDto
                accessToken  = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresAt    = result.ExpiresAt,

                // Nested user object expected by Web LoginResponseDto.User
                user = new
                {
                    id        = result.User.Id,
                    username  = result.User.Username,
                    email     = result.User.Email,
                    firstName = result.User.FirstName,
                    lastName  = result.User.LastName,
                    fullName  = $"{result.User.FirstName} {result.User.LastName}",
                    isActive  = result.User.IsActive
                },

                // Shop-role list expected by Web LoginResponseDto.ShopRoles
                shopRoles = result.ShopRoles,

                // Convenience fields for the JS auth-context
                shopId   = defaultShop?.ShopId,
                shopName = defaultShop?.ShopName,
                role     = defaultShop?.RoleName,
                roleId   = defaultShop?.RoleId
            };

            return SuccessResponse(response, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            return ErrorResponse("An error occurred during login", 500);
        }
    }

    // Registration endpoint removed - use user management API instead

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New JWT token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (result == null)
                return UnauthorizedResponse("Invalid or expired refresh token");

            return SuccessResponse(result, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return ErrorResponse("An error occurred during token refresh", 500);
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    /// <returns>Success message</returns>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        try
        {
            var username = GetCurrentUsername();
            
            // Clear the HttpOnly cookie
            Response.Cookies.Delete("AuthToken");
            
            _logger.LogInformation("User {Username} logged out", username);

            return SuccessResponse("Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return ErrorResponse("An error occurred during logout", 500);
        }
    }

    /// <summary>
    /// Validate current token
    /// </summary>
    /// <returns>Token validation result</returns>
    [Authorize]
    [HttpGet("validate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        try
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var role = GetCurrentUserRole();
            var shopId = GetCurrentShopId();

            return SuccessResponse(new
            {
                userId,
                username,
                role,
                shopId,
                isValid = true
            }, "Token is valid");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token validation");
            return ErrorResponse("An error occurred during token validation", 500);
        }
    }

    /// <summary>
    /// Get current user context from JWT token
    /// This endpoint provides user/shop information without exposing the token
    /// </summary>
    /// <returns>User and shop context</returns>
    [Authorize]
    [HttpGet("context")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetContext()
    {
        try
        {
            var context = new
            {
                userId = GetCurrentUserId(),
                userName = GetCurrentUsername(),
                email = GetCurrentUserEmail(),
                firstName = GetCurrentUserFirstName(),
                lastName = GetCurrentUserLastName(),
                fullName = GetCurrentUserFullName(),
                shopId = GetCurrentShopId(),
                shopName = GetCurrentShopName(),
                role = GetCurrentUserRole(),
                roleId = GetCurrentRoleId(),
                isAuthenticated = true
            };

            return SuccessResponse(context, "Context retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user context");
            return ErrorResponse("An error occurred while retrieving user context", 500);
        }
    }
}

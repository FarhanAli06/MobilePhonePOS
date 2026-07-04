using Empire.Application.DTOs.Auth;
using System.Security.Claims;

namespace Empire.Application.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// Generate JWT token for authenticated user
    /// </summary>
    string GenerateToken(UserDto user, List<UserShopRoleDto> shopRoles);
    
    /// <summary>
    /// Validate JWT token and return claims principal
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
    
    /// <summary>
    /// Refresh JWT token
    /// </summary>
    string RefreshToken(string token);
    
    /// <summary>
    /// Extract user ID from token
    /// </summary>
    int? GetUserIdFromToken(string token);
    
    /// <summary>
    /// Extract shop ID from token
    /// </summary>
    int? GetShopIdFromToken(string token);
    
    /// <summary>
    /// Check if token is expired
    /// </summary>
    bool IsTokenExpired(string token);
}

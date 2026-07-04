using System.Security.Claims;
using Empire.Domain.Entities;

namespace Empire.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user, IEnumerable<UserShopRole> shopRoles);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiration(string token);
}


using Empire.Application.DTOs.Auth;
using Empire.Application.Interfaces;
using Empire.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Empire.Infrastructure.Services.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public string GenerateToken(UserDto user, List<UserShopRoleDto> shopRoles)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            // Additional user context claims
            new Claim("UserId", user.Id.ToString()),
            new Claim("FirstName", user.FirstName ?? string.Empty),
            new Claim("LastName", user.LastName ?? string.Empty),
            new Claim("FullName", $"{user.FirstName} {user.LastName}")
        };

        // Add shop roles as claims
        foreach (var shopRole in shopRoles)
        {
            claims.Add(new Claim($"ShopId_{shopRole.ShopId}", "true"));
            claims.Add(new Claim($"Shop_{shopRole.ShopId}_Role", shopRole.RoleName));
            claims.Add(new Claim($"Shop_{shopRole.ShopId}_RoleId", shopRole.RoleId.ToString()));
        }

        // Add current shop ID (first shop as default)
        if (shopRoles.Any())
        {
            var firstShop = shopRoles.First();
            claims.Add(new Claim("CurrentShopId", firstShop.ShopId.ToString()));
            claims.Add(new Claim("CurrentShopName", firstShop.ShopName));
            claims.Add(new Claim("CurrentRole", firstShop.RoleName));
            claims.Add(new Claim("CurrentRoleId", firstShop.RoleId.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string RefreshToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null)
        {
            throw new SecurityTokenException("Invalid token");
        }

        // Extract user info from existing token
        var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var username = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
        var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        var name = principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
        {
            throw new SecurityTokenException("Invalid token claims");
        }

        var names = name?.Split(' ') ?? new[] { "", "" };
        var user = new UserDto
        {
            Id = int.Parse(userId),
            Username = username,
            Email = email ?? "",
            FirstName = names.Length > 0 ? names[0] : "",
            LastName = names.Length > 1 ? names[1] : ""
        };

        // Extract shop roles from claims
        var shopRoles = new List<UserShopRoleDto>();
        var shopClaims = principal.Claims.Where(c => c.Type.StartsWith("ShopId_")).ToList();
        
        foreach (var shopClaim in shopClaims)
        {
            var shopId = int.Parse(shopClaim.Type.Replace("ShopId_", ""));
            var roleName = principal.FindFirst($"Shop_{shopId}_Role")?.Value ?? "Viewer";
            var roleId = int.Parse(principal.FindFirst($"Shop_{shopId}_RoleId")?.Value ?? "5");
            var shopName = principal.FindFirst("CurrentShopName")?.Value ?? "Unknown";

            shopRoles.Add(new UserShopRoleDto
            {
                ShopId = shopId,
                ShopName = shopName,
                RoleId = roleId,
                RoleName = roleName
            });
        }

        return GenerateToken(user, shopRoles);
    }

    public int? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        var userIdClaim = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public int? GetShopIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        var shopIdClaim = principal?.FindFirst("CurrentShopId")?.Value;
        return int.TryParse(shopIdClaim, out var shopId) ? shopId : null;
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}

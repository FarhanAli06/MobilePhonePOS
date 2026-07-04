using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Empire.Web.Services.Auth;

public class TokenStorageService : ITokenStorageService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _env;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private const string TokenCookieName = "EmpirePOS.AuthToken";
    
    // In-memory cache for performance
    private string? _cachedToken;

    public TokenStorageService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env)
    {
        _httpContextAccessor = httpContextAccessor;
        _env = env;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public void SetToken(string token)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        // Store in HTTP-only cookie.
        // Secure=true is required in production (HTTPS). In development the API
        // runs on HTTP, so Secure must be false or the browser will never send
        // the cookie back and every server-side GetToken() call returns null.
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // false in dev (HTTP), true in prod (HTTPS)
            SameSite = SameSiteMode.Lax,    // Lax allows same-site HTTP redirects
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        };

        context.Response.Cookies.Append(TokenCookieName, token, cookieOptions);

        // Cache in memory
        _cachedToken = token;
    }

    public string? GetToken()
    {
        // Return cached token if available
        if (!string.IsNullOrEmpty(_cachedToken))
        {
            return _cachedToken;
        }

        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        // Try to get from cookie
        if (context.Request.Cookies.TryGetValue(TokenCookieName, out var token))
        {
            _cachedToken = token;
            return token;
        }

        return null;
    }

    public void RemoveToken()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        // Remove cookie
        context.Response.Cookies.Delete(TokenCookieName);

        // Clear cache
        _cachedToken = null;
    }

    public bool IsAuthenticated()
    {
        var token = GetToken();
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo > DateTime.UtcNow;
        }
        catch
        {
            return false;
        }
    }

    public int? GetUserId()
    {
        var claims = GetClaims();
        var userIdClaim = claims?.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public int? GetShopId()
    {
        var claims = GetClaims();
        var shopIdClaim = claims?.FindFirst("CurrentShopId")?.Value;
        return int.TryParse(shopIdClaim, out var shopId) ? shopId : null;
    }

    public string? GetUsername()
    {
        var claims = GetClaims();
        return claims?.FindFirst("unique_name")?.Value;
    }

    private ClaimsPrincipal? GetClaims()
    {
        var token = GetToken();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }
}

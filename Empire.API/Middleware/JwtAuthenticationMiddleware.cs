using Empire.Application.Interfaces;
using System.Net;

namespace Empire.API.Middleware;

/// <summary>
/// Middleware to validate JWT tokens on every API request.
/// Reads the token from the Authorization header first, then falls back to the
/// HttpOnly cookie named "EmpirePOS.AuthToken" (used by the browser client).
/// </summary>
public class JwtAuthenticationMiddleware
{
    private const string AuthCookieName = "EmpirePOS.AuthToken";

    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService)
    {
        // Skip authentication for certain paths
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (ShouldSkipAuthentication(path))
        {
            await _next(context);
            return;
        }

        // 1. Try Authorization header first (server-to-server / Swagger / mobile)
        var token = ExtractTokenFromHeader(context);

        // 2. Fall back to HttpOnly cookie (browser client via auth-context.js)
        if (string.IsNullOrEmpty(token))
        {
            token = ExtractTokenFromCookie(context);
        }

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("No JWT token found in request to {Path}", path);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Authentication required. No token provided."
            });
            return;
        }

        // Validate token
        var principal = jwtTokenService.ValidateToken(token);
        if (principal == null)
        {
            _logger.LogWarning("Invalid JWT token for request to {Path}", path);
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid or expired token."
            });
            return;
        }

        // Set user principal
        context.User = principal;

        // Log successful authentication
        var userId   = principal.FindFirst("sub")?.Value;
        var username = principal.FindFirst("unique_name")?.Value;
        _logger.LogInformation(
            "User {Username} (ID: {UserId}) authenticated for {Path}",
            username, userId, path);

        await _next(context);
    }

    // ─── Token extraction helpers ─────────────────────────────────────────────

    private static string? ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader)) return null;

        return authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..].Trim()
            : null;
    }

    private static string? ExtractTokenFromCookie(HttpContext context)
    {
        context.Request.Cookies.TryGetValue(AuthCookieName, out var token);
        return string.IsNullOrEmpty(token) ? null : token;
    }

    // ─── Public paths ─────────────────────────────────────────────────────────

    private static bool ShouldSkipAuthentication(string path)
    {
        var publicPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/forgot-password",
            "/api/auth/reset-password",
            "/swagger",
            "/health",
            "/api/health"
        };

        return publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}

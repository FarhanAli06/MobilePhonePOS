using Empire.Application.Interfaces;
using System.Net;

namespace Empire.API.Middleware;

/// <summary>
/// Middleware to validate JWT tokens on every API request.
/// Reads the token from the Authorization header first, then falls back to the
/// HttpOnly cookie named "EmpirePOS.AuthToken" (used by the browser client).
///
/// Public paths (no token required) are listed in <see cref="PublicPaths"/> and
/// <see cref="PublicPrefixes"/>. Any path that starts with one of the prefixes,
/// or exactly matches one of the paths, is allowed through without authentication.
/// </summary>
public class JwtAuthenticationMiddleware
{
    private const string AuthCookieName = "EmpirePOS.AuthToken";

    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    // Exact paths that are always public (case-insensitive)
    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",                          // Root — Swagger UI index in development
        "/index.html",                // Swagger UI static entry point
        "/favicon.ico",               // Browser favicon request
        "/health",                    // ASP.NET health check endpoint
        "/api/health",                // Alternate health check path
    };

    // Path prefixes that are always public (case-insensitive, StartsWith match)
    private static readonly string[] PublicPrefixes = new[]
    {
        "/api/auth/",                 // Login, register, forgot-password, reset-password
        "/swagger",                   // Swagger UI and JSON (/swagger, /swagger/v1/swagger.json, etc.)
        "/health",                    // Health checks
    };

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService)
    {
        var path = context.Request.Path.Value ?? "/";

        if (ShouldSkipAuthentication(path))
        {
            await _next(context);
            return;
        }

        // 1. Try Authorization header first (server-to-server / Swagger / mobile)
        var token = ExtractTokenFromHeader(context);

        // 2. Fall back to HttpOnly cookie (browser client via auth-context.js)
        if (string.IsNullOrEmpty(token))
            token = ExtractTokenFromCookie(context);

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

        // Attach validated principal so controllers can read claims
        context.User = principal;

        var userId   = principal.FindFirst("sub")?.Value;
        var username = principal.FindFirst("unique_name")?.Value;
        _logger.LogDebug(
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

    // ─── Public path check ────────────────────────────────────────────────────

    private static bool ShouldSkipAuthentication(string path)
    {
        // Exact match
        if (PublicPaths.Contains(path))
            return true;

        // Prefix match (case-insensitive)
        foreach (var prefix in PublicPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

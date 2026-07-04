using Empire.Web.Constants;
using Empire.Web.Services.Auth;
using System.Net;
using System.Net.Http.Headers;

namespace Empire.Web.Handlers;

/// <summary>
/// HTTP message handler that automatically injects JWT token into all API requests.
/// Reads the token from the ASP.NET session first (most reliable for server-side calls),
/// then falls back to the HttpOnly cookie via ITokenStorageService.
/// Public endpoints (login, register, etc.) are excluded so that a stale or expired
/// token never causes a 401 on those routes.
/// </summary>
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly ITokenStorageService _tokenStorageService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticationDelegatingHandler> _logger;

    // Endpoints that must never receive an Authorization header.
    // Sending a stale/expired token to these endpoints causes the API's JWT middleware
    // to return 401 before the [AllowAnonymous] attribute is evaluated.
    private static readonly string[] _publicPaths =
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/forgot-password",
        "/api/auth/reset-password"
    };

    public AuthenticationDelegatingHandler(
        ITokenStorageService tokenStorageService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticationDelegatingHandler> logger)
    {
        _tokenStorageService = tokenStorageService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Skip token injection for public endpoints
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isPublic = _publicPaths.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isPublic)
        {
            // If the caller already set an explicit Authorization header (e.g. GetWithTokenAsync
            // passes the token directly at login time before the session is committed),
            // respect it and do NOT overwrite it.
            if (request.Headers.Authorization != null)
            {
                _logger.LogDebug("Authorization header already set by caller; skipping injection for: {Method} {Uri}",
                    request.Method, request.RequestUri);
            }
            else
            {
                // Resolve token: session first (always available server-side), then cookie fallback
                var token = ResolveToken();

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    _logger.LogDebug("Added Authorization header to request: {Method} {Uri}",
                        request.Method, request.RequestUri);
                }
                else
                {
                    _logger.LogWarning("No token available for request: {Method} {Uri}",
                        request.Method, request.RequestUri);
                }
            }
        }

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized responses
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized response from {Uri}", request.RequestUri);

            // If the token is flagged as expired, remove it so the next request
            // forces a fresh login rather than retrying with a bad token.
            if (response.Headers.Contains("Token-Expired"))
            {
                _logger.LogInformation("Token expired, removing from storage");
                _tokenStorageService.RemoveToken();
            }
        }

        return response;
    }

    /// <summary>
    /// Resolves the JWT token using a reliable priority order:
    /// 1. ASP.NET session (always available on the same server-side request)
    /// 2. ITokenStorageService (reads from HttpOnly cookie)
    /// </summary>
    private string? ResolveToken()
    {
        // 1. Try session first — most reliable for server-side outbound calls
        try
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                var sessionToken = ctx.Session.GetString(SessionKeys.AuthToken);
                if (!string.IsNullOrEmpty(sessionToken))
                    return sessionToken;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read token from session; falling back to cookie.");
        }

        // 2. Fall back to cookie-based storage
        return _tokenStorageService.GetToken();
    }
}

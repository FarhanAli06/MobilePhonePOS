using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;

namespace Empire.API.Middleware;

/// <summary>
/// ASP.NET Core middleware that captures every incoming HTTP request and its
/// response, then persists a structured <see cref="RequestLog"/> record to SQL
/// via <see cref="IRequestLogService"/>.
///
/// Design decisions:
/// - Request body is buffered (EnableBuffering) so it can be read without
///   consuming the stream for downstream handlers.
/// - Response body is intercepted via a MemoryStream wrapper so we can read
///   the response bytes before they are sent to the client.
/// - Only error responses (4xx/5xx) have their response body captured to
///   keep storage usage low for high-traffic deployments.
/// - Body content is truncated to 4 KB to prevent runaway storage growth.
/// - Logging failures are swallowed so they never break the main pipeline.
/// - Health-check and static-file paths are skipped.
/// </summary>
public class RequestLoggingMiddleware
{
    private const int MaxBodyBytes = 4096;

    private static readonly string[] SkipPaths =
    [
        "/health",
        "/swagger",
        "/favicon.ico"
    ];

    // Endpoints whose request bodies should be redacted (contain credentials)
    private static readonly HashSet<string> SensitiveEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/refresh",
        "/api/auth/register"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    private readonly IServiceScopeFactory _scopeFactory;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip non-API / health paths
        if (SkipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        // ── Buffer the request body so it can be read multiple times ──────────
        context.Request.EnableBuffering();
        var requestBody = await ReadBodyAsync(context.Request.Body);
        context.Request.Body.Position = 0;

        // Redact sensitive endpoints
        if (SensitiveEndpoints.Contains(path))
            requestBody = "[REDACTED]";

        // ── Capture the response body ─────────────────────────────────────────
        var originalResponseBody = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        string? responseBody = null;
        string? errorMessage = null;
        int statusCode = 200;

        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;

            // Only capture response body for errors
            if (statusCode >= 400)
            {
                responseBuffer.Position = 0;
                responseBody = await ReadStreamAsync(responseBuffer);
            }
        }
        catch (Exception ex)
        {
            statusCode = 500;
            errorMessage = ex.ToString().SafeTruncate(MaxBodyBytes);
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, path);
        }
        finally
        {
            // Copy buffered response back to the original stream
            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;
            sw.Stop();
        }

        // ── Extract identity from JWT claims ──────────────────────────────────
        var userId   = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirstValue("sub");
        var username = context.User.FindFirstValue(ClaimTypes.Name)
                    ?? context.User.FindFirstValue("unique_name")
                    ?? context.User.FindFirstValue("username");
        var shopIdClaim = context.User.FindFirstValue("CurrentShopId")
                       ?? context.User.FindFirstValue("shopId");
        int.TryParse(shopIdClaim, out var shopId);

        // ── Capture selected request headers (no auth tokens) ─────────────────
        var headers = BuildHeaderString(context.Request.Headers);

        var log = new RequestLog
        {
            Source              = "API",
            HttpMethod          = context.Request.Method,
            Path                = path.SafeTruncate(500),
            QueryString         = context.Request.QueryString.Value?.SafeTruncate(1000),
            RequestBody         = requestBody?.SafeTruncate(MaxBodyBytes),
            RequestHeaders      = headers,
            UserId              = userId?.SafeTruncate(100),
            Username            = username?.SafeTruncate(100),
            ShopId              = shopId > 0 ? shopId : null,
            IpAddress           = GetClientIp(context).SafeTruncate(50),
            UserAgent           = context.Request.Headers.UserAgent.ToString().SafeTruncate(500),
            StatusCode          = statusCode,
            ResponseBody        = responseBody,
            ElapsedMilliseconds = sw.ElapsedMilliseconds,
            RequestedAtUtc      = DateTime.UtcNow,
            ErrorMessage        = errorMessage
        };

        // Fire-and-forget using a fresh DI scope — the request scope has ended here
        // so we must NOT use the scoped logService parameter.
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IRequestLogService>();
                await svc.LogAsync(log);
            }
            catch (Exception ex)
            {
                // Log to console/file — never crash the app but surface the error
                _logger.LogError(ex, "[RequestLogging] Failed to persist log for {Method} {Path} → {StatusCode}",
                    log.HttpMethod, log.Path, log.StatusCode);
            }
        });

        // Also write a structured log line for console/file sinks
        if (statusCode >= 400)
        {
            _logger.LogWarning(
                "[API] {Method} {Path} → {StatusCode} ({ElapsedMs}ms) User={Username} Shop={ShopId}",
                log.HttpMethod, path, statusCode, sw.ElapsedMilliseconds, username ?? "-", shopId);
        }
        else
        {
            _logger.LogInformation(
                "[API] {Method} {Path} → {StatusCode} ({ElapsedMs}ms) User={Username} Shop={ShopId}",
                log.HttpMethod, path, statusCode, sw.ElapsedMilliseconds, username ?? "-", shopId);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<string?> ReadBodyAsync(Stream body)
    {
        if (!body.CanRead) return null;
        try
        {
            using var reader = new StreamReader(body, Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            var text = await reader.ReadToEndAsync();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch { return null; }
    }

    private static async Task<string?> ReadStreamAsync(Stream stream)
    {
        if (!stream.CanRead) return null;
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var text = await reader.ReadToEndAsync();
            return string.IsNullOrWhiteSpace(text) ? null : text.SafeTruncate(MaxBodyBytes);
        }
        catch { return null; }
    }

    private static string BuildHeaderString(IHeaderDictionary headers)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in headers)
        {
            if (key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
             || key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
             || key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                continue;

            sb.Append(key).Append(": ").Append(value).Append(" | ");
            if (sb.Length > 1900) break;
        }
        return sb.ToString().TrimEnd(' ', '|');
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

// ── Extension helpers ─────────────────────────────────────────────────────────

internal static class LogStringExtensions
{
    /// <summary>Truncate a string to at most <paramref name="maxLength"/> characters.</summary>
    internal static string SafeTruncate(this string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}

/// <summary>Extension method to register the middleware.</summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestLoggingMiddleware>();
}

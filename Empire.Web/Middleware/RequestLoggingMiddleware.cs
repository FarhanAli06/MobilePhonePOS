using System.Diagnostics;

namespace Empire.Web.Middleware;

/// <summary>
/// Middleware to log all HTTP requests and responses
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var user = context.User?.Identity?.Name ?? "Anonymous";

        try
        {
            _logger.LogInformation(
                "Request started: {Method} {Path} by {User}",
                requestMethod, requestPath, user);

            await _next(context);

            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(logLevel,
                "Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms by {User}",
                requestMethod, requestPath, statusCode, stopwatch.ElapsedMilliseconds, user);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Request failed: {Method} {Path} - Duration: {Duration}ms by {User} - Error: {ErrorMessage}",
                requestMethod, requestPath, stopwatch.ElapsedMilliseconds, user, ex.Message);
            
            throw;
        }
    }
}

/// <summary>
/// Extension method to add request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}

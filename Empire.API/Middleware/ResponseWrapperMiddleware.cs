using System.Text.Json;

namespace Empire.API.Middleware;

/// <summary>
/// Middleware to automatically wrap all API responses in a standardized format
/// </summary>
public class ResponseWrapperMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseWrapperMiddleware> _logger;

    public ResponseWrapperMiddleware(RequestDelegate next, ILogger<ResponseWrapperMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Create a new memory stream to capture the response
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware
            await _next(context);

            // Reset the stream position
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            // Wrap the response if it's not already wrapped
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                // Success response
                if (!IsAlreadyWrapped(responseText))
                {
                    var wrappedResponse = new
                    {
                        success = true,
                        message = "Success",
                        data = string.IsNullOrEmpty(responseText) ? null : JsonSerializer.Deserialize<object>(responseText),
                        statusCode = context.Response.StatusCode,
                        timestamp = DateTime.UtcNow,
                        traceId = context.TraceIdentifier
                    };

                    var wrappedJson = JsonSerializer.Serialize(wrappedResponse);
                    context.Response.ContentLength = null; // Reset content length
                    context.Response.Body = originalBodyStream;
                    await context.Response.WriteAsync(wrappedJson);
                }
                else
                {
                    // Already wrapped, just copy to original stream
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            else
            {
                // Error response - copy as is (will be handled by exception middleware)
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResponseWrapperMiddleware");
            context.Response.Body = originalBodyStream;
            throw;
        }
    }

    private bool IsAlreadyWrapped(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return false;

        try
        {
            var doc = JsonDocument.Parse(responseText);
            return doc.RootElement.TryGetProperty("success", out _) &&
                   doc.RootElement.TryGetProperty("timestamp", out _);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Extension method to add ResponseWrapperMiddleware to the pipeline
/// </summary>
public static class ResponseWrapperMiddlewareExtensions
{
    public static IApplicationBuilder UseResponseWrapper(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResponseWrapperMiddleware>();
    }
}

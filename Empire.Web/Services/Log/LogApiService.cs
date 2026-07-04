using Empire.Web.Services.Http;

namespace Empire.Web.Services.Log;

/// <summary>
/// Calls the API's /api/logs endpoint to retrieve and manage request logs.
/// </summary>
public class LogApiService : ILogApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<LogApiService> _logger;
    private const string BaseEndpoint = "/api/logs";

    public LogApiService(IHttpClientService httpClient, ILogger<LogApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LogPagedResult?> GetLogsAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? source = null,
        string? method = null,
        string? path = null,
        string? username = null,
        int? shopId = null,
        int? statusCode = null,
        bool? errorsOnly = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        try
        {
            var qs = BuildQueryString(pageNumber, pageSize, source, method, path,
                username, shopId, statusCode, errorsOnly, from, to);

            return await _httpClient.GetAsync<LogPagedResult>($"{BaseEndpoint}?{qs}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching logs from API");
            return null;
        }
    }

    public async Task<int> PurgeLogsAsync(int days = 90)
    {
        try
        {
            var result = await _httpClient.DeleteAsync($"{BaseEndpoint}/purge?days={days}");
            return result ? days : 0; // simplified — actual count comes from API response
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging logs");
            return 0;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildQueryString(
        int pageNumber, int pageSize,
        string? source, string? method, string? path,
        string? username, int? shopId, int? statusCode,
        bool? errorsOnly, DateTime? from, DateTime? to)
    {
        var parts = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(source))   parts.Add($"source={Uri.EscapeDataString(source)}");
        if (!string.IsNullOrWhiteSpace(method))   parts.Add($"method={Uri.EscapeDataString(method)}");
        if (!string.IsNullOrWhiteSpace(path))     parts.Add($"path={Uri.EscapeDataString(path)}");
        if (!string.IsNullOrWhiteSpace(username)) parts.Add($"username={Uri.EscapeDataString(username)}");
        if (shopId.HasValue)      parts.Add($"shopId={shopId.Value}");
        if (statusCode.HasValue)  parts.Add($"statusCode={statusCode.Value}");
        if (errorsOnly.HasValue)  parts.Add($"errorsOnly={errorsOnly.Value.ToString().ToLower()}");
        if (from.HasValue)        parts.Add($"from={from.Value:O}");
        if (to.HasValue)          parts.Add($"to={to.Value:O}");

        return string.Join("&", parts);
    }
}

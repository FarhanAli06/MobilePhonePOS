namespace Empire.Web.Services.Log;

/// <summary>
/// Service for querying the API's request log endpoint.
/// </summary>
public interface ILogApiService
{
    /// <summary>
    /// Retrieves a paged, filtered list of request log entries.
    /// </summary>
    Task<LogPagedResult?> GetLogsAsync(
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
        DateTime? to = null);

    /// <summary>
    /// Purges log entries older than the specified number of days.
    /// </summary>
    Task<int> PurgeLogsAsync(int days = 90);
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class LogPagedResult
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<LogEntryDto> Items { get; set; } = [];
}

public class LogEntryDto
{
    public long Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public string? RequestBody { get; set; }
    public string? RequestHeaders { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public int? ShopId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsError { get; set; }
}

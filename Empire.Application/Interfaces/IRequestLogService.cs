using Empire.Domain.Entities;

namespace Empire.Application.Interfaces;

/// <summary>
/// Application-layer service for writing and querying HTTP audit logs.
/// </summary>
public interface IRequestLogService
{
    /// <summary>Persist a log entry asynchronously (non-blocking).</summary>
    Task LogAsync(RequestLog log);

    /// <summary>
    /// Retrieve a paged, filtered list of log entries.
    /// </summary>
    Task<(IEnumerable<RequestLog> Items, int TotalCount)> GetLogsAsync(
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

    /// <summary>Purge log entries older than <paramref name="days"/> days.</summary>
    Task<int> PurgeOldLogsAsync(int days = 90);
}

using Empire.Domain.Entities;

namespace Empire.Domain.Interfaces;

/// <summary>
/// Repository for persisting and querying HTTP request/response audit logs.
/// </summary>
public interface IRequestLogRepository
{
    /// <summary>Persist a single log entry (fire-and-forget safe).</summary>
    Task AddAsync(RequestLog log);

    /// <summary>
    /// Query logs with optional filters, ordered by most-recent first.
    /// All parameters are optional — omit to return all logs.
    /// </summary>
    Task<(IEnumerable<RequestLog> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? source = null,
        string? method = null,
        string? path = null,
        string? username = null,
        int? shopId = null,
        int? statusCode = null,
        bool? errorsOnly = null,
        DateTime? from = null,
        DateTime? to = null);

    /// <summary>Delete log entries older than the given UTC date.</summary>
    Task<int> PurgeOlderThanAsync(DateTime cutoffUtc);
}

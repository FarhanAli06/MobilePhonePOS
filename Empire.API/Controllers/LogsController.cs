using Empire.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empire.API.Controllers;

/// <summary>
/// API endpoint for querying the RequestLogs audit table.
/// Restricted to authenticated users only.
/// </summary>
[Authorize]
public class LogsController : BaseApiController
{
    private readonly IRequestLogService _logService;

    public LogsController(IRequestLogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// GET /api/logs
    /// Returns a paged, filtered list of request log entries.
    /// </summary>
    /// <param name="pageNumber">1-based page number (default: 1)</param>
    /// <param name="pageSize">Records per page (default: 50, max: 200)</param>
    /// <param name="source">Filter by source: "API" or "WEB"</param>
    /// <param name="method">Filter by HTTP method: GET, POST, PUT, DELETE</param>
    /// <param name="path">Partial path filter (e.g. "repairs")</param>
    /// <param name="username">Partial username filter</param>
    /// <param name="shopId">Filter by shop ID</param>
    /// <param name="statusCode">Filter by exact HTTP status code</param>
    /// <param name="errorsOnly">When true, return only 4xx/5xx responses</param>
    /// <param name="from">Start of date range (UTC, ISO 8601)</param>
    /// <param name="to">End of date range (UTC, ISO 8601)</param>
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? source = null,
        [FromQuery] string? method = null,
        [FromQuery] string? path = null,
        [FromQuery] string? username = null,
        [FromQuery] int? shopId = null,
        [FromQuery] int? statusCode = null,
        [FromQuery] bool? errorsOnly = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        // Clamp page size to prevent runaway queries
        pageSize = Math.Clamp(pageSize, 1, 200);
        pageNumber = Math.Max(1, pageNumber);

        var (items, totalCount) = await _logService.GetLogsAsync(
            pageNumber, pageSize, source, method, path,
            username, shopId, statusCode, errorsOnly, from, to);

        var result = new
        {
            pageNumber,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            items = items.Select(l => new
            {
                l.Id,
                l.Source,
                l.HttpMethod,
                l.Path,
                l.QueryString,
                l.RequestBody,
                l.RequestHeaders,
                l.UserId,
                l.Username,
                l.ShopId,
                l.IpAddress,
                l.UserAgent,
                l.StatusCode,
                l.ResponseBody,
                l.ElapsedMilliseconds,
                l.RequestedAtUtc,
                l.ErrorMessage,
                isError = l.StatusCode >= 400
            })
        };

        return SuccessResponse(result, $"Retrieved {totalCount} log entries");
    }

    /// <summary>
    /// DELETE /api/logs/purge?days=90
    /// Deletes log entries older than the specified number of days.
    /// Admin only.
    /// </summary>
    [HttpDelete("purge")]
    public async Task<IActionResult> PurgeLogs([FromQuery] int days = 90)
    {
        if (days < 1)
            return ErrorResponse("Days must be at least 1", 400);

        var deleted = await _logService.PurgeOldLogsAsync(days);
        return SuccessResponse(new { deleted }, $"Purged {deleted} log entries older than {days} days");
    }
}

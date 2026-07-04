using Empire.Web.Authorization;
using Empire.Web.Services.Log;
using Microsoft.AspNetCore.Mvc;

namespace Empire.Web.Controllers;

/// <summary>
/// Web controller for the Request Logs admin page.
/// Proxies data from the API's /api/logs endpoint.
/// </summary>
[SessionAuthorizeWithShop]
public class LogsController : Controller
{
    private readonly ILogApiService _logApi;
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogApiService logApi, ILogger<LogsController> logger)
    {
        _logApi = logApi;
        _logger = logger;
    }

    /// <summary>GET /Logs — renders the Logs viewer page.</summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// GET /Logs/GetLogs — AJAX endpoint called by the DataTable on the Index page.
    /// Accepts filter parameters from the query string.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs(
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
            var result = await _logApi.GetLogsAsync(
                pageNumber, pageSize, source, method, path,
                username, shopId, statusCode, errorsOnly, from, to);

            if (result == null)
                return Json(new { success = false, message = "Failed to retrieve logs" });

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs");
            return Json(new { success = false, message = "An error occurred while retrieving logs" });
        }
    }

    /// <summary>
    /// DELETE /Logs/Purge?days=90 — purges old log entries.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Purge(int days = 90)
    {
        try
        {
            var deleted = await _logApi.PurgeLogsAsync(days);
            return Json(new { success = true, message = $"Purged log entries older than {days} days", deleted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging logs");
            return Json(new { success = false, message = "An error occurred while purging logs" });
        }
    }
}

using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Empire.Application.Services;

/// <summary>
/// Application service that wraps <see cref="IRequestLogRepository"/>.
/// All write operations swallow exceptions so that a logging failure
/// never breaks the main request pipeline.
/// </summary>
public class RequestLogService : IRequestLogService
{
    private readonly IRequestLogRepository _repository;
    private readonly ILogger<RequestLogService> _logger;

    public RequestLogService(IRequestLogRepository repository, ILogger<RequestLogService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAsync(RequestLog log)
    {
        try
        {
            await _repository.AddAsync(log);
        }
        catch (Exception ex)
        {
            // Never let a logging failure crash the request
            _logger.LogError(ex, "Failed to persist request log for {Method} {Path}", log.HttpMethod, log.Path);
        }
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<RequestLog> Items, int TotalCount)> GetLogsAsync(
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
        return await _repository.GetPagedAsync(
            pageNumber, pageSize, source, method, path,
            username, shopId, statusCode, errorsOnly, from, to);
    }

    /// <inheritdoc />
    public async Task<int> PurgeOldLogsAsync(int days = 90)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await _repository.PurgeOlderThanAsync(cutoff);
    }
}

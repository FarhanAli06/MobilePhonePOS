using Microsoft.EntityFrameworkCore;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for <see cref="RequestLog"/>.
/// Injected as a scoped service; the middleware creates a fresh DI scope
/// before resolving this repository so it always gets a fresh DbContext.
/// </summary>
public class RequestLogRepository : IRequestLogRepository
{
    private readonly EmpireDbContext _context;

    public RequestLogRepository(EmpireDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(RequestLog log)
    {
        _context.RequestLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<RequestLog> Items, int TotalCount)> GetPagedAsync(
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
        DateTime? to = null)
    {
        var query = _context.RequestLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(l => l.Source == source);

        if (!string.IsNullOrWhiteSpace(method))
            query = query.Where(l => l.HttpMethod == method.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(path))
            query = query.Where(l => l.Path.Contains(path));

        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(l => l.Username != null && l.Username.Contains(username));

        if (shopId.HasValue)
            query = query.Where(l => l.ShopId == shopId.Value);

        if (statusCode.HasValue)
            query = query.Where(l => l.StatusCode == statusCode.Value);

        if (errorsOnly == true)
            query = query.Where(l => l.StatusCode >= 400);

        if (from.HasValue)
            query = query.Where(l => l.RequestedAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.RequestedAtUtc <= to.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.RequestedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<int> PurgeOlderThanAsync(DateTime cutoffUtc)
    {
        return await _context.RequestLogs
            .Where(l => l.RequestedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync();
    }
}

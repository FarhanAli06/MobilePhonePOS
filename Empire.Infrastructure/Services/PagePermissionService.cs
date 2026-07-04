using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Page;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Infrastructure.Data;
namespace Empire.Infrastructure.Services;
public class PagePermissionService : IPagePermissionService
{
    private readonly EmpireDbContext _context;
    public PagePermissionService(EmpireDbContext context)
    {
        _context = context;
    }
    // ── Helpers ────────────────────────────────────────────────────────────
    private static PageDto MapPage(Page p) => new()
    {
        Id             = p.Id,
        Name           = p.Name,
        PageKey        = p.PageKey,
        ControllerName = p.ControllerName,
        ActionName     = p.ActionName,
        Icon           = p.Icon,
        ParentPageId   = p.ParentPageId,
        DisplayOrder   = p.DisplayOrder,
        GroupName      = p.GroupName,
        IsActive       = p.IsActive
    };
    // ── Public methods ─────────────────────────────────────────────────────
    public async Task<IEnumerable<PageDto>> GetAllPagesAsync()
    {
        var pages = await _context.Pages
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
        return pages.Select(MapPage);
    }
    public async Task<IEnumerable<PageDto>> GetPageTreeAsync()
    {
        var pages = await _context.Pages
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
        var dict = pages.ToDictionary(p => p.Id, MapPage);
        foreach (var dto in dict.Values)
        {
            if (dto.ParentPageId.HasValue && dict.TryGetValue(dto.ParentPageId.Value, out var parent))
                parent.ChildPages.Add(dto);
        }
        return dict.Values.Where(p => p.ParentPageId == null);
    }
    public async Task<IEnumerable<PageDto>> GetPagesWithUserPermissionsAsync(int userId, int shopId)
    {
        var pages = await _context.Pages
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
        var grantedIds = await _context.UserPagePermissions
            .Where(up => up.UserId == userId && up.ShopId == shopId && up.IsGranted && !up.IsDeleted)
            .Select(up => up.PageId)
            .ToListAsync();
        var grantedSet = grantedIds.ToHashSet();
        return pages.Select(p =>
        {
            var dto = MapPage(p);
            // Re-use IsActive as "is granted" flag for this specific query
            dto.IsActive = grantedSet.Contains(p.Id);
            return dto;
        });
    }
    public async Task<UserPermissionSummaryDto?> GetUserPermissionSummaryAsync(int userId, int shopId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null) return null;
        var grantedPageIds = await _context.UserPagePermissions
            .Where(up => up.UserId == userId && up.ShopId == shopId && up.IsGranted && !up.IsDeleted)
            .Select(up => up.PageId)
            .ToListAsync();
        var grantedPages = await _context.Pages
            .Where(p => grantedPageIds.Contains(p.Id) && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
        return new UserPermissionSummaryDto
        {
            UserId          = user.Id,
            Username        = user.Username,
            FullName        = $"{user.FirstName} {user.LastName}".Trim(),
            GrantedPages    = grantedPages.Select(MapPage).ToList(),
            GrantedPageKeys = grantedPages.Select(p => p.PageKey).ToList()
        };
    }
    public async Task<bool> AssignPagesToUserAsync(int userId, int shopId, List<int> pageIds, int grantedByUserId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null) return false;
        // Load existing permissions for this user in this shop only
        var existing = await _context.UserPagePermissions
            .Where(up => up.UserId == userId && up.ShopId == shopId)
            .ToListAsync();
        var existingDict = existing.ToDictionary(up => up.PageId);
        var requestedSet = pageIds.ToHashSet();
        // Grant new / re-activate revoked
        foreach (var pageId in requestedSet)
        {
            if (existingDict.TryGetValue(pageId, out var perm))
            {
                perm.IsGranted       = true;
                perm.IsDeleted       = false;
                perm.GrantedDate     = DateTime.UtcNow;
                perm.GrantedByUserId = grantedByUserId;
            }
            else
            {
                _context.UserPagePermissions.Add(new UserPagePermission
                {
                    UserId          = userId,
                    ShopId          = shopId,
                    PageId          = pageId,
                    IsGranted       = true,
                    GrantedByUserId = grantedByUserId,
                    GrantedDate     = DateTime.UtcNow
                });
            }
        }
        // Revoke permissions no longer in the list (for this shop only)
        foreach (var perm in existing.Where(p => !requestedSet.Contains(p.PageId)))
        {
            perm.IsGranted = false;
        }
        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<List<string>> GetGrantedPageKeysAsync(int userId, int shopId)
    {
        return await _context.UserPagePermissions
            .Where(up => up.UserId == userId && up.ShopId == shopId && up.IsGranted && !up.IsDeleted)
            .Join(_context.Pages,
                  up => up.PageId,
                  p  => p.Id,
                  (up, p) => p.PageKey)
            .ToListAsync();
    }
}

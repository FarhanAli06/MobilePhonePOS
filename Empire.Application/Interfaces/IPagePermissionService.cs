using Empire.Application.DTOs.Page;

namespace Empire.Application.Interfaces;

public interface IPagePermissionService
{
    /// <summary>Returns all active pages (flat list).</summary>
    Task<IEnumerable<PageDto>> GetAllPagesAsync();

    /// <summary>Returns all active pages structured as a tree (parents with children).</summary>
    Task<IEnumerable<PageDto>> GetPageTreeAsync();

    /// <summary>Returns all pages with a flag indicating whether the user has been granted access for the given shop.</summary>
    Task<IEnumerable<PageDto>> GetPagesWithUserPermissionsAsync(int userId, int shopId);

    /// <summary>Returns the permission summary for a user including granted page keys for the given shop.</summary>
    Task<UserPermissionSummaryDto?> GetUserPermissionSummaryAsync(int userId, int shopId);

    /// <summary>
    /// Replaces the user's page assignments for the given shop with the supplied list.
    /// Pages in <paramref name="pageIds"/> are granted; all others are revoked.
    /// </summary>
    Task<bool> AssignPagesToUserAsync(int userId, int shopId, List<int> pageIds, int grantedByUserId);

    /// <summary>Returns the flat list of page keys the user is allowed to access in the given shop.</summary>
    Task<List<string>> GetGrantedPageKeysAsync(int userId, int shopId);
}
